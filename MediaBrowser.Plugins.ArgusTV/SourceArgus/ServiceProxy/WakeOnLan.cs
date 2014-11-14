/*
 *	Copyright (C) 2007-2014 ARGUS TV
 *	http://www.argus-tv.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */
using System;
using System.Globalization;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace ArgusTV.ServiceProxy
{
    internal static class WakeOnLan
    {
        private const string _defaultSubnetMask = "255.255.255.0";

		static WakeOnLan()
		{
			InitializeUnixPing();
		}

        public static string GetIPAddress(string hostName)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            foreach (IPAddress ipAddress in addresses)
            {
                if (!IPAddress.IsLoopback(ipAddress)
                    && ipAddress.AddressFamily == AddressFamily.InterNetwork
                    && Ping(ipAddress))
                {
                    return ipAddress.ToString();
                }
            }
            return String.Empty;
        }

        public static bool Ping(string ipString)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(ipString, out ipAddress))
            {
				return Ping(ipAddress);
            }
            return false;
        }

        public static bool Ping(IPAddress ipAddress)
        {
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				return UnixPing(ipAddress, 500, null, new PingOptions()) == IPStatus.Success;
			}
            using (Ping ping = new Ping())
            {
                return (ping.Send(ipAddress, 500).Status == IPStatus.Success);
            }
        }

        public static void EnsureServerAwake(ServerSettings serverSettings)
        {
            // Check if wake-on-lan is turned on and if the server does *not* respond to a ping.
            // In that case we will try to wake it up.
            if (serverSettings.WakeOnLan.Enabled
                && !String.IsNullOrEmpty(serverSettings.WakeOnLan.MacAddresses)
                && !String.IsNullOrEmpty(serverSettings.WakeOnLan.IPAddress)
                && !Ping(serverSettings.WakeOnLan.IPAddress))
            {
                string[] macAddresses = serverSettings.WakeOnLan.MacAddresses.Split(';');
                if (WakeUp(macAddresses, serverSettings.WakeOnLan.IPAddress, serverSettings.WakeOnLan.TimeoutSeconds))
                {
                    // Wait one additional second after a successful ping.
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        public static bool WakeUp(string[] macAddresses, string serverIPAddress, int timeoutSeconds)
        {
            bool result = false;
            bool pingAfterWake = true;

            IPAddress ipAddress;
            if (!IPAddress.TryParse(serverIPAddress, out ipAddress))
            {
                ipAddress = IPAddress.Broadcast;
                pingAfterWake = false;
            }

            foreach (string macAddress in macAddresses)
            {
                SendWakeOnLan(macAddress, ipAddress);
            }

            if (pingAfterWake)
            {
                DateTime startPingTime = DateTime.Now;
                for (; ; )
                {
                    if (Ping(ipAddress))
                    {
                        result = true;
                        break;
                    }
                    TimeSpan span = DateTime.Now - startPingTime;
                    if (span.TotalSeconds > timeoutSeconds)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static void SendWakeOnLan(string mac, IPAddress ipAddress)
        {
            byte[] macBytes = ConvertMacAddress(mac);
            if (macBytes != null)
            {
                byte[] packet = CreateWakeOnLanPacket(macBytes);

                IPAddress subnetMask = FindSubnetMask(ipAddress);

                IPEndPoint endPoint = new IPEndPoint(ApplySubnetMask(ipAddress, subnetMask), 9);

                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    socket.SendTo(packet, endPoint);
                    socket.Close();
                }
            }
        }

        private static byte[] CreateWakeOnLanPacket(byte[] macBytes)
        {
            byte[] packet = new byte[21 * 6];
            for (int index = 0; index < 6; index++)
            {
                packet[index] = 0xFF;
            }
            for (int count = 1; count <= 20; count++)
            {
                for (int index = 0; index < 6; index++)
                {
                    packet[count * 6 + index] = macBytes[index];
                }
            }
            return packet;
        }

        private static byte[] ConvertMacAddress(string mac)
        {
            if (mac.Length != 12)
            {
                return null;
            }
            byte[] macBytes = new byte[6];
            for (int index = 0; index < 6; index++)
            {
                byte byteValue;
                if (!byte.TryParse(mac.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byteValue))
                {
                    return null;
                }
                macBytes[index] = byteValue;
            }
            return macBytes;
        }

        private static IPAddress FindSubnetMask(IPAddress serverIPAddress)
        {
			if (Environment.OSVersion.Platform == PlatformID.Win32NT
			    || Environment.OSVersion.Platform == PlatformID.Win32Windows)
			{
	            using (ManagementObjectSearcher query = new ManagementObjectSearcher(
	                "Select IPAddress,IPSubnet from Win32_NetworkAdapterConfiguration where IPEnabled=TRUE"))
	            using (ManagementObjectCollection mgmntObjects = query.Get())
	            {
	                foreach (ManagementObject mo in mgmntObjects)
	                {
	                    string[] ipaddresses = (string[])mo["IPAddress"];
	                    string[] subnets = (string[])mo["IPSubnet"];
	                    for (int index = 0; index < Math.Min(ipaddresses.Length, subnets.Length); index++)
	                    {
	                        IPAddress localIP;
	                        IPAddress subnetIP;
	                        if (IPAddress.TryParse(ipaddresses[index], out localIP)
	                            && IPAddress.TryParse(subnets[index], out subnetIP))
	                        {
	                            if (ApplySubnetMask(localIP, subnetIP).Equals(ApplySubnetMask(serverIPAddress, subnetIP)))
	                            {
	                                return subnetIP;
	                            }
	                        }
	                    }
	                }
	            }
			}
            return IPAddress.Parse(_defaultSubnetMask);
        }

        private static IPAddress ApplySubnetMask(IPAddress ipAddress, IPAddress ipNetMask)
        {
            byte[] ipBytes = ipAddress.GetAddressBytes();
            byte[] maskBytes = ipNetMask.GetAddressBytes();
            for (int index = 0; index < 4; index++)
            {
                ipBytes[index] |= (byte)(maskBytes[index] ^ 0xFF);
            }
            return new IPAddress(ipBytes);
        }

		#region Unix Ping

		private static readonly string [] _pingBinPaths = new string [] {
			"/bin/ping",
			"/sbin/ping",
			"/usr/sbin/ping",
			"/system/bin/ping"
		};

		private static string _pingBinPath;

		private static void InitializeUnixPing()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				// Since different Unix systems can have different path to bin, we try some
				// of the known ones.
				foreach (string ping_path in _pingBinPaths)
				{
					if (File.Exists(ping_path))
					{
						_pingBinPath = ping_path;
						break;
					}
				}
			}
		}

		private static IPStatus UnixPing(IPAddress address, int timeout, byte [] buffer, PingOptions options)
		{
			DateTime sentTime = DateTime.UtcNow;

			Process ping = new Process();
			string args = BuildPingArgs(address, timeout, options);
			long trip_time = 0;

			ping.StartInfo.FileName = _pingBinPath;
			ping.StartInfo.Arguments = args;
			ping.StartInfo.CreateNoWindow = true;
			ping.StartInfo.UseShellExecute = false;
			ping.StartInfo.RedirectStandardOutput = true;
			ping.StartInfo.RedirectStandardError = true;

			try
			{
				ping.Start();

				#pragma warning disable 219
				string stdout = ping.StandardOutput.ReadToEnd();
				string stderr = ping.StandardError.ReadToEnd();
				#pragma warning restore 219

				trip_time = (long)(DateTime.UtcNow - sentTime).TotalMilliseconds;
				if (!ping.WaitForExit(timeout) || (ping.HasExited && ping.ExitCode == 2))
				{
					return IPStatus.TimedOut;
				}
				if (ping.ExitCode == 1)
				{
					return IPStatus.TtlExpired;
				}
			}
			catch (Exception)
			{
				return IPStatus.Unknown;
			}
			finally
			{
				if (ping != null)
				{
					if (!ping.HasExited)
						ping.Kill();
					ping.Dispose();
				}
			}

			return IPStatus.Success;
		}

		private static string BuildPingArgs (IPAddress address, int timeout, PingOptions options)
		{
			CultureInfo culture = CultureInfo.InvariantCulture;
			StringBuilder args = new StringBuilder ();
			uint t = Convert.ToUInt32 (Math.Floor ((timeout + 1000) / 1000.0));
			bool is_mac = ((int) Environment.OSVersion.Platform == 6);
			if (!is_mac)
				args.AppendFormat (culture, "-q -n -c {0} -w {1} -t {2} -M ", 1, t, options.Ttl);
			else
				args.AppendFormat (culture, "-q -n -c {0} -t {1} -o -m {2} ", 1, t, options.Ttl);
			if (!is_mac)
				args.Append (options.DontFragment ? "do " : "dont ");
			else if (options.DontFragment)
				args.Append ("-D ");

			args.Append (address.ToString ());

			return args.ToString ();
		}

		#endregion
    }
}
