using MediaBrowser.Model.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TVHeadEnd.Helper;

namespace TVHeadEnd.HTSP
{
    public class HTSMessage
    {
        public const long HTSP_VERSION = 10;
        private const byte HMF_MAP = 1;
        private const byte HMF_S64 = 2;
        private const byte HMF_STR = 3;
        private const byte HMF_BIN = 4;
        private const byte HMF_LIST = 5;

        private readonly Dictionary<string, object> _dict;
        private ILogger _logger = null;
        private byte[] _data = null;

        public HTSMessage()
        {
            _dict = new Dictionary<string, object>();
        }

        public void putField(string name, object value)
        {
            if (value != null)
            {
                _dict[name] = value;
                _data = null;
            }
        }

        public void removeField(string name)
        {
            _dict.Remove(name);
            _data = null;
        }

        public Dictionary<string, object>.Enumerator GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public string Method
        {
            set
            {
                _dict["method"] = value;
                _data = null;
            }
            get
            {
                return getString("method", "");
            }
        }


        public bool containsField(string name)
        {
            return _dict.ContainsKey(name);
        }

        public System.Numerics.BigInteger getBigInteger(string name)
        {
            try
            {
                return (System.Numerics.BigInteger)_dict[name];
            }
            catch(InvalidCastException ice)
            {
                _logger.Fatal("[TVHclient] Caught InvalidCastException for field name '" + name + "'. Expected  'System.Numerics.BigInteger' but got '" +
                    _dict[name].GetType() + "'");
                throw ice;
            }
        }

        public long getLong(string name)
        {
            return (long)getBigInteger(name);
        }

        public long getLong(string name, long std)
        {
            if (!containsField(name))
            {
                return std;
            }
            return getLong(name);
        }

        public int getInt(string name)
        {
            return (int)getBigInteger(name);
        }

        public int getInt(string name, int std)
        {
            if (!containsField(name))
            {
                return std;
            }
            return getInt(name);
        }

        public string getString(string name, string std)
        {
            if (!containsField(name))
            {
                return std;
            }
            return getString(name);
        }

        public string getString(string name)
        {
            object obj = _dict[name];
            if (obj == null)
            {
                return null;
            }
            return obj.ToString();
        }

        public IList<long?> getLongList(string name)
        {
            List<long?> list = new List<long?>();

            if (!containsField(name))
            {
                return list;
            }

            foreach (object obj in (IList)_dict[name])
            {
                if (obj is System.Numerics.BigInteger)
                {
                    list.Add((long)((System.Numerics.BigInteger)obj));
                }
            }

            return list;
        }

        internal IList<long?> getLongList(string name, IList<long?> std)
        {
            if (!containsField(name))
            {
                return std;
            }

            return getLongList(name);
        }

        public IList<int?> getIntList(string name)
        {
            List<int?> list = new List<int?>();

            if (!containsField(name))
            {
                return list;
            }

            foreach (object obj in (IList)_dict[name])
            {
                if (obj is System.Numerics.BigInteger)
                {
                    list.Add((int)((System.Numerics.BigInteger)obj));
                }
            }

            return list;
        }

        internal IList<int?> getIntList(string name, IList<int?> std)
        {
            if (!containsField(name))
            {
                return std;
            }

            return getIntList(name);
        }

        public IList getList(string name)
        {
            return (IList)_dict[name];
        }

        public byte[] getByteArray(string name)
        {
            return (byte[])_dict[name];
        }

        public DateTime getDate(string name)
        {
            return new DateTime(getLong(name) * 1000);
        }

        public byte[] BuildBytes()
        {
            if(_data != null)
            {
                return _data;
            }

            byte[] buf = new byte[0];

            // calc data
            byte[] data = serializeBinary(_dict);

            // calc length
            int len = data.Length;
            byte[] tmpByte = new byte[1];
            tmpByte[0] = unchecked((byte)((len >> 24) & 0xFF));
            buf = buf.Concat(tmpByte).ToArray();
            tmpByte[0] = unchecked((byte)((len >> 16) & 0xFF));
            buf = buf.Concat(tmpByte).ToArray();
            tmpByte[0] = unchecked((byte)((len >> 8) & 0xFF));
            buf = buf.Concat(tmpByte).ToArray();
            tmpByte[0] = unchecked((byte)((len) & 0xFF));
            buf = buf.Concat(tmpByte).ToArray();

            // append data
            buf = buf.Concat(data).ToArray();

            return buf;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\nHTSMessage:\n");
            sb.Append("  <dump>\n");
            sb.Append(getValueString(_dict, "    "));
            sb.Append("  </dump>\n\n");
            return sb.ToString();
        }

        private string getValueString(object value, string pad)
        {
            if (value is byte[])
            {
                StringBuilder sb = new StringBuilder();
                byte[] bVal = (byte[])value;
                for (int ii = 0; ii < bVal.Length; ii++)
                {
                    sb.Append(bVal[ii]);
                    //sb.Append(" (" + Convert.ToString(bVal[ii], 2).PadLeft(8, '0') + ")");
                    sb.Append(", ");
                }
                return sb.ToString();
            }
            else if (value is IDictionary)
            {
                StringBuilder sb = new StringBuilder();
                IDictionary dictVal = (IDictionary)value;
                foreach (object key in dictVal.Keys)
                {
                    object currValue = dictVal[key];
                    sb.Append(pad + key + " : " + getValueString(currValue, pad + "  ") + "\n");
                }
                return sb.ToString();
            }
            else if (value is ICollection)
            {
                StringBuilder sb = new StringBuilder();
                ICollection colVal = (ICollection)value;
                foreach (object tmpObj in colVal)
                {
                    sb.Append(getValueString(tmpObj, pad) + ", ");
                }
                return sb.ToString();
            }
            return "" + value;
        }

        private byte[] serializeBinary(IDictionary map)
        {
            byte[] buf = new byte[0];
            foreach (object key in map.Keys)
            {
                object value = map[key];
                byte[] sub = serializeBinary(key.ToString(), value);
                buf = buf.Concat(sub).ToArray();
            }
            return buf;
        }

        private byte[] serializeBinary(ICollection list)
        {
            byte[] buf = new byte[0];
            foreach (object value in list)
            {
                byte[] sub = serializeBinary("", value);
                buf = buf.Concat(sub).ToArray();
            }
            return buf;
        }


        private byte[] serializeBinary(string name, object value)
        {
            byte[] bName = GetBytes(name);
            byte[] bData = new byte[0];
            byte type;

            if (value is string)
            {
                type = HTSMessage.HMF_STR;
                bData = GetBytes(((string)value));
            }
            else if (value is System.Numerics.BigInteger)
            {
                type = HTSMessage.HMF_S64;
                bData = toByteArray((System.Numerics.BigInteger)value);
            }
            else if (value is int?)
            {
                type = HTSMessage.HMF_S64;
                bData = toByteArray((int)value);
            }
            else if (value is long?)
            {
                type = HTSMessage.HMF_S64;
                bData = toByteArray((long)value);
            }
            else if (value is byte[])
            {
                type = HTSMessage.HMF_BIN;
                bData = (byte[])value;
            }
            else if (value is IDictionary)
            {
                type = HTSMessage.HMF_MAP;
                bData = serializeBinary((IDictionary)value);
            }
            else if (value is ICollection)
            {
                type = HTSMessage.HMF_LIST;
                bData = serializeBinary((ICollection)value);
            }
            else if (value == null)
            {
                throw new IOException("HTSP doesn't support null values");
            }
            else
            {
                throw new IOException("Unhandled class for " + name + ": " + value + " (" + value.GetType().Name + ")");
            }

            byte[] buf = new byte[1 + 1 + 4 + bName.Length + bData.Length];
            buf[0] = type;
            buf[1] = unchecked((byte)(bName.Length & 0xFF));
            buf[2] = unchecked((byte)((bData.Length >> 24) & 0xFF));
            buf[3] = unchecked((byte)((bData.Length >> 16) & 0xFF));
            buf[4] = unchecked((byte)((bData.Length >> 8) & 0xFF));
            buf[5] = unchecked((byte)((bData.Length) & 0xFF));

            Array.Copy(bName, 0, buf, 6, bName.Length);
            Array.Copy(bData, 0, buf, 6 + bName.Length, bData.Length);
            
            return buf;
        }

        private byte[] toByteArray(System.Numerics.BigInteger big)
        {
            byte[] b = BitConverter.GetBytes((long)big);
            byte[] b1 = new byte[0];
            Boolean tail = false;
            for (int ii = 0; ii < b.Length; ii++)
            {
                if (b[ii] != 0 || !tail)
                {
                    tail = true;
                    b1 = b1.Concat(new byte[] { b[ii] }).ToArray();
                }
            }
            if (b1.Length == 0)
            {
                b1 = new byte[1];
            }
            return b1;
        }

        public static HTSMessage parse(byte[] data, ILogger logger)
        {
            if (data.Length < 4)
            {
                logger.Error("[HTSMessage.parse(byte[])] Really to short");
                return null;
            }

            long len = uIntToLong(data[0], data[1], data[2], data[3]);
            //Message not fully read
            if (data.Length < len + 4)
            {
                logger.Error("[HTSMessage.parse(byte[])] not enough data for len: " + len);
                return null;
            }

            //drops 4 bytes (length information)
            byte[] messageData = new byte[len];
            Array.Copy(data, 4, messageData, 0, len);

            HTSMessage msg = deserializeBinary(messageData);

            msg._logger = logger;
            msg._data = data;

            return msg;
        }

        public static long uIntToLong(byte b1, byte b2, byte b3, byte b4)
        {
            long i = 0;
            i <<= 8;
            i ^= b1 & 0xFF;
            i <<= 8;
            i ^= b2 & 0xFF;
            i <<= 8;
            i ^= b3 & 0xFF;
            i <<= 8;
            i ^= b4 & 0xFF;
            return i;
        }

        private static System.Numerics.BigInteger toBigInteger(byte[] b)
        {
            byte[] b1 = new byte[8];
            for (int ii = 0; ii < b.Length; ii++)
            {
                b1[ii] = b[ii];
            }
            long lValue = BitConverter.ToInt64(b1, 0);
            return new System.Numerics.BigInteger(lValue);
        }

        private static HTSMessage deserializeBinary(byte[] messageData)
        {
            byte type, namelen;
            long datalen;

            HTSMessage msg = new HTSMessage();
            int cnt = 0;

            ByteBuffer buf = new ByteBuffer(messageData);
            while (buf.hasRemaining())
            {
                type = buf.get();
                namelen = buf.get();
                datalen = uIntToLong(buf.get(), buf.get(), buf.get(), buf.get());

                if (buf.Length() < namelen + datalen)
                {
                    throw new IOException("Buffer limit exceeded");
                }

                //Get the key for the map (the name)
                string name = null;
                if (namelen == 0)
                {
                    name = Convert.ToString(cnt++);
                }
                else
                {
                    byte[] bName = new byte[namelen];
                    buf.get(bName);
                    name = NewString(bName);
                }

                //Get the actual content
                object obj = null;
                byte[] bData = new byte[datalen];
                buf.get(bData);

                switch (type)
                {
                    case HTSMessage.HMF_STR:
                        {
                            obj = NewString(bData);
                            break;
                        }
                    case HMF_BIN:
                        {
                            obj = bData;
                            break;
                        }
                    case HMF_S64:
                        {
                            obj = toBigInteger(bData);
                            break;
                        }
                    case HMF_MAP:
                        {
                            obj = deserializeBinary(bData);
                            break;
                        }
                    case HMF_LIST:
                        {
                            obj = new List<object>(deserializeBinary(bData)._dict.Values);
                            break;
                        }
                    default:
                        throw new IOException("Unknown data type");
                }
                msg.putField(name, obj);
            }
            return msg;
        }


        private static string NewString(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private byte[] GetBytes(string s)
        {
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            byte[] bytes = new byte[encoding.GetByteCount(s)];
            encoding.GetBytes(s, 0, s.Length, bytes, 0);
            return bytes;
        }
    }
}