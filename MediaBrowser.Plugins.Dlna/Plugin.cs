using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Plugins.Dlna.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MediaBrowser.Plugins.Dlna
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        //these are Neptune values, they probably belong in the managed wrapper somewhere, but they aren't
        //techincally theres 50 to 100 of these values, but these 3 seem to be the most useful
        private const int NEP_Failure = -1;
        private const int NEP_NotImplemented = -2012;
        private const int NEP_Success = 0;

        private Platinum.UPnP _Upnp;
        private Platinum.MediaConnect _PlatinumServer;
        private User _CurrentUser;


        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "DLNA Server"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "DLNA Server"; }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        public Plugin()
            : base()
        {
            Instance = this;
        }

        /// <summary>
        /// Initializes the on server.
        /// </summary>
        /// <param name="isFirstRun">if set to <c>true</c> [is first run].</param>
        protected override void InitializeOnServer(bool isFirstRun)
        {
            base.InitializeOnServer(isFirstRun);

            Kernel.ReloadCompleted += Kernel_ReloadCompleted;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        }

        /// <summary>
        /// Handles the AssemblyResolve event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ResolveEventArgs" /> instance containing the event data.</param>
        /// <returns>Assembly.</returns>
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var askedAssembly = new AssemblyName(args.Name);

            var resourcePath = "MediaBrowser.Plugins.Dlna.Assemblies." + askedAssembly.Name + ".dll";

            using (var stream = GetType().Assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    Logger.Info("Loading assembly from resource {0}", resourcePath);

                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        memoryStream.Position = 0;

                        return Assembly.Load(memoryStream.ToArray());
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Disposes the on server.
        /// </summary>
        /// <param name="dispose">if set to <c>true</c> [dispose].</param>
        protected override void DisposeOnServer(bool dispose)
        {
            if (dispose)
            {
                Kernel.ReloadCompleted -= Kernel_ReloadCompleted;
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                DisposeDlnaServer();
            }

            base.DisposeOnServer(dispose);
        }

        /// <summary>
        /// Handles the ReloadCompleted event of the Kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Kernel_ReloadCompleted(object sender, EventArgs e)
        {
            InitializeDlnaServer();
        }

        /// <summary>
        /// Initializes the dlna server.
        /// </summary>
        private void InitializeDlnaServer()
        {
            this.SetupUPnPServer();
        }

        /// <summary>
        /// Disposes the dlna server.
        /// </summary>
        private void DisposeDlnaServer()
        {
            this.CleanupUPnPServer();
        }

        private void SetupUPnPServer()
        {

            this._Upnp = new Platinum.UPnP();
            // Will need a config setting to set the friendly name of the upnp server
            //this._PlatinumServer = new Platinum.MediaConnect("MB3 UPnP", "MB3UPnP", 1901);
            if (this.Configuration.DlnaPortNumber.HasValue)
                this._PlatinumServer = new Platinum.MediaConnect(this.Configuration.FriendlyDlnaName, "MB3UPnP", this.Configuration.DlnaPortNumber.Value);
            else
                this._PlatinumServer = new Platinum.MediaConnect(this.Configuration.FriendlyDlnaName);

            this._PlatinumServer.BrowseMetadata += new Platinum.MediaConnect.BrowseMetadataDelegate(server_BrowseMetadata);
            this._PlatinumServer.BrowseDirectChildren += new Platinum.MediaConnect.BrowseDirectChildrenDelegate(server_BrowseDirectChildren);
            this._PlatinumServer.ProcessFileRequest += new Platinum.MediaConnect.ProcessFileRequestDelegate(server_ProcessFileRequest);
            this._PlatinumServer.SearchContainer += new Platinum.MediaConnect.SearchContainerDelegate(server_SearchContainer);

            this._Upnp.AddDeviceHost(this._PlatinumServer);
            this._Upnp.Start();
        }

        private void CleanupUPnPServer()
        {
            if (this._Upnp != null && this._Upnp.Running)
                this._Upnp.Stop();

            if (this._PlatinumServer != null)
            {
                this._PlatinumServer.BrowseMetadata -= new Platinum.MediaConnect.BrowseMetadataDelegate(server_BrowseMetadata);
                this._PlatinumServer.BrowseDirectChildren -= new Platinum.MediaConnect.BrowseDirectChildrenDelegate(server_BrowseDirectChildren);
                this._PlatinumServer.ProcessFileRequest -= new Platinum.MediaConnect.ProcessFileRequestDelegate(server_ProcessFileRequest);
                this._PlatinumServer.SearchContainer -= new Platinum.MediaConnect.SearchContainerDelegate(server_SearchContainer);

                this._PlatinumServer.Dispose();
                this._PlatinumServer = null;
            }

            if (this._Upnp != null)
            {
                this._Upnp.Dispose();
                this._Upnp = null;
            }
        }

        private int server_BrowseMetadata(Platinum.Action action, string object_id, string filter, int starting_index, int requested_count, string sort_criteria, Platinum.HttpRequestContext context)
        {
            Logger.Info("BrowseMetadata Entered - Parameters: action:{0} object_id:{1} filter:{2} starting_index:{3} requested_count:{4} sort_criteria:{5} context:{6}",
                action.ToLogString(), object_id, filter, starting_index, requested_count, sort_criteria, context.ToLogString());

            //nothing much seems to call BrowseMetadata so far
            //but perhaps that is because we aren't handing out the correct info for the client to call this... I don't know

            //PS3 calls it
            //Parameters: action:Action Name:Browse Description:Platinum.ActionDescription Arguments: object_id:0 
            //filter:@id,upnp:class,res,res@protocolInfo,res@av:authenticationUri,res@size,dc:title,upnp:albumArtURI,res@dlna:ifoFileURI,res@protection,res@bitrate,res@duration,res@sampleFrequency,res@bitsPerSample,res@nrAudioChannels,res@resolution,res@colorDepth,dc:date,av:dateTime,upnp:artist,upnp:album,upnp:genre,dc:contributer,upnp:storageFree,upnp:storageUsed,upnp:originalTrackNumber,dc:publisher,dc:language,dc:region,dc:description,upnp:toc,@childCount,upnp:albumArtURI@dlna:profileID,res@dlna:cleartextSize 
            //starting_index:0 requested_count:1 sort_criteria: context:HttpRequestContext LocalAddress:HttpRequestContext.SocketAddress IP:192.168.1.56 Port:1845 RemoteAddress:HttpRequestContext.SocketAddress IP:192.168.1.40 Port:49277 Request:http://192.168.1.56:1845/ContentDirectory/7c6b1b90-872b-2cda-3c5c-21a0e430ce5e/control.xml Signature:PS3



            if (object_id == "0")
            {
                var root = new Platinum.MediaContainer();
                root.Title = "Root";
                root.ObjectID = "0";
                root.ParentID = "-1";
                root.Class = new Platinum.ObjectClass("object.container.storageFolder", "");

                var didl = Platinum.Didl.header + root.ToDidl(filter) + Platinum.Didl.footer;
                action.SetArgumentValue("Result", didl);
                action.SetArgumentValue("NumberReturned", "1");
                action.SetArgumentValue("TotalMatches", "1");

                // update ID may be wrong here, it should be the one of the container?
                action.SetArgumentValue("UpdateId", "1");

                return NEP_Success;
            }
            else
            {
                return NEP_Failure;
            }
        }
        private int server_BrowseDirectChildren(Platinum.Action action, String object_id, String filter, Int32 starting_index, Int32 requested_count, String sort_criteria, Platinum.HttpRequestContext context)
        {
            Logger.Info("BrowseDirectChildren Entered - Parameters: action:{0} object_id:{1} filter:{2} starting_index:{3} requested_count:{4} sort_criteria:{5} context:{6}",
                action.ToLogString(), object_id, filter, starting_index, requested_count, sort_criteria, context.ToLogString());

            //WMP doesn't care how many results we return and what type they are
            //Xbox360 Music App is unknown, it calls SearchContainer and stops, not sure what will happen once we return it results
            //XBox360 Video App has fairly specific filter string and it need it - if you serve it music (mp3), it'll put music in the video list, so we have to do our own filtering

            //XBox360 Video App
            //  action: "Browse"
            //  object_id: "15"
            //  filter: "dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec"
            //  starting_index: 0
            //  requested_count: 100
            //  sort_criteria: "+upnp:class,+dc:title"
            //
            //the wierd thing about the filter is that there isn't much in it that says "only give me video"... except...
            //if we look at the doc available here: http://msdn.microsoft.com/en-us/library/windows/hardware/gg487545.aspx which describes the way WMP does its DLNA serving (not clienting)
            //doc is also a search cached google docs here: https://docs.google.com/viewer?a=v&q=cache:tnrdpTFCc84J:download.microsoft.com/download/0/0/b/00bba048-35e6-4e5b-a3dc-36da83cbb0d1/NetCompat_WMP11.docx+&hl=en&gl=au&pid=bl&srcid=ADGEESiBSKE1ZJeWmgYVOkKmRJuYaSL3_50KL1o6Ugp28JL1Ytq-2QbEeu6xFD8rbWcX35ZG4d7qPQnzqURGR5vig79S2Arj5umQNPnLeJo1k5_iWYbqPejeMHwwAv0ATmq2ynoZCBNL&sig=AHIEtbQ2qZJ8xMXLZYBWHHerezzXShKoVg
            //it describes object 15 as beeing Root/Video/Folders which can contain object.storageFolder
            //so perhaps thats what is saying 'only give me video'
            //I'm just not sure if those folders listed with object IDs are all well known across clients or if these ones are WMP specific
            //if they are device specific but also significant, then that might explain why Plex goes to the trouble of having configurable client device profiles for its DLNA server

            
            //XBOX360 Video
            //BrowseDirectChildren Entered - Parameters: 
            //action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", 
            //Arguments:[ ] }  
            //object_id:15 
            //filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec 
            //starting_index:0 
            //requested_count:1000 
            //sort_criteria:+upnp:class,+dc:title 
            //context: { LocalAddress:{ IP:192.168.1.56, Port:1143 }, RemoteAddress:{ IP:192.168.1.27, Port:43702 }, Request:"http://192.168.1.56:1143/ContentDirectory/41d4bbfc-aff3-f300-7e69-14762558a3ba/control.xml", Signature:XBox }

            //a working log from Xbox360 Video App
            //no metadata (other than Title) that works, but the xbox doesn't seem to support anything more than that
            //image/thumbnail serving works if you feed it the direct path, current 'api url' doesn't work, perhaps because it has no extension

            /*
            2013-02-24 22:46:52.3962, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:15 filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:48040 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:46:54.5234, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:af99c816-0c3b-4770-099e-d5c039548e4f filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:11538 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:46:56.6050, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:5da43a95-3c34-3c3a-e123-50f20623d650 filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:33536 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:46:58.8234, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:26d7de4d-2e5d-7f8c-6d61-71674afc6503 filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:59852 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:46:59.9894, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:e51e6a3e-fe62-4bae-04c9-e8b6efb41b1e filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:8285 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:47:01.0216, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:d42fe500-8ac1-7476-6445-eec48085cc4a filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:46319 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:47:01.1244, Info, App, ProcessFileRequest Entered - Parameters: context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:9842 }, Request:"http://192.168.1.56:1733/7cb7f497-234f-05e3-64c0-926ff07d3fa6?albumArt=true", Signature:XBox } response:Platinum.HttpResponse
            2013-02-24 22:47:01.1244, Info, App, ProcessFileRequest Entered - Parameters: context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:65477 }, Request:"http://192.168.1.56:1733/76ab6bd1-a914-b126-45fc-e84a6402f8b0?albumArt=true", Signature:XBox } response:Platinum.HttpResponse
            2013-02-24 22:47:08.9738, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:da407897-748f-065a-f020-d9dbaf598ff2 filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:37906 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            2013-02-24 22:47:09.0699, Info, App, ProcessFileRequest Entered - Parameters: context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:9842 }, Request:"http://192.168.1.56:1733/1ce95963-d31a-3052-8cf1-f31e934bd4fe?albumArt=true", Signature:XBox } response:Platinum.HttpResponse
            2013-02-24 22:47:24.0908, Info, App, BrowseDirectChildren Entered - Parameters: action: { Name:"Browse", Description:" { Name:"Browse", Arguments:[ ] } ", Arguments:[ ] }  object_id:90a8b701-b1ca-325d-e00f-d3f60267584d filter:dc:title,res,res@protection,res@duration,res@bitrate,upnp:genre,upnp:actor,res@microsoft:codec starting_index:0 requested_count:1000 sort_criteria:+upnp:class,+dc:title context: { LocalAddress:{ IP:192.168.1.56, Port:1733 }, RemoteAddress:{ IP:192.168.1.27, Port:44378 }, Request:"http://192.168.1.56:1733/ContentDirectory/944ef00a-1bd9-d8f2-02ab-9a5de207da75/control.xml", Signature:XBox }
            */

            Model.ModelBase objectIDMatch = null;

            var root = new Model.Root(this.CurrentUser);
            if (string.Equals(object_id, "0", StringComparison.OrdinalIgnoreCase))
                objectIDMatch = root;
            else
                objectIDMatch = root.GetChildRecursive(object_id);

            if (objectIDMatch != null)
            {
                var children = objectIDMatch.Children;
                if (children != null)
                {
                    int itemCount = 0;
                    var didl = Platinum.Didl.header;
                    foreach (var child in children)
                    {
                        using (var item = child.MediaObject)
                        {
                            if (item != null)
                            {
                                AddContextInfo(item, child.MainMediaResource, child.Id, child.Extension, context);

                                string test;
                                test = item.ToDidl(filter);
                                didl += item.ToDidl(filter);
                                itemCount++;
                            }
                        }
                    }
                    didl += Platinum.Didl.footer;

                    action.SetArgumentValue("Result", didl);
                    action.SetArgumentValue("NumberReturned", itemCount.ToString());
                    action.SetArgumentValue("TotalMatches", itemCount.ToString());

                    // update ID may be wrong here, it should be the one of the container?
                    action.SetArgumentValue("UpdateId", "1");

                    return NEP_Success;
                }
            }
            return NEP_Failure;
        }
        private int server_ProcessFileRequest(Platinum.HttpRequestContext context, Platinum.HttpResponse response)
        {
            Logger.Info("ProcessFileRequest Entered - Parameters: context:{0} response:{1}",
                context.ToLogString(), response);

            Uri uri = context.Request.URI;
            var id = uri.AbsolutePath.TrimStart('/');
            Guid itemID;
            if (Guid.TryParseExact(id, "D", out itemID))
            {
                var item = this.CurrentUser.RootFolder.FindItemById(itemID, this.CurrentUser);

                if (item != null)
                {
                    //this is how the Xbox 360 Video app asks for artwork, it tacks this query string onto its request
                    //?albumArt=true
                    if (uri.Query == "?albumArt=true")
                    {
                        if (!string.IsNullOrWhiteSpace(item.PrimaryImagePath))
                        {
                            //let see if we can serve artwork like this to the Xbox 360 Video App
                            //var url = Kernel.HttpServerUrlPrefix.Replace("+", context.LocalAddress.ip) + "Items/" + item.Id.ToString() + "/Images/Primary";
                            //Logger.Debug("Serving image url:" + url);
                            //Platinum.MediaServer.SetResponseFilePath(context, response, url);
                            //the 360 Video App will not accept the above url as an image, perhaps if it had an image extension it might work
                            Platinum.MediaServer.SetResponseFilePath(context, response, item.PrimaryImagePath);
                        }
                    }
                    else
                        Platinum.MediaServer.SetResponseFilePath(context, response, item.Path);
                    //this does not work for WMP
                    //Platinum.MediaServer.SetResponseFilePath(context, response, Kernel.HttpServerUrlPrefix.Replace("+", context.LocalAddress.ip) + "/api/video.ts?id=" + item.Id.ToString());

                    return NEP_Success;
                }
            }
            return NEP_Failure;
        }
        private int server_SearchContainer(Platinum.Action action, string object_id, string searchCriteria, string filter, int starting_index, int requested_count, string sort_criteria, Platinum.HttpRequestContext context)
        {
            Logger.Info("SearchContainer Entered - Parameters: action:{0} object_id:{1} searchCriteria:{7} filter:{2} starting_index:{3} requested_count:{4} sort_criteria:{5} context:{6}",
                action.ToLogString(), object_id, filter, starting_index, requested_count, sort_criteria, context.ToLogString(), searchCriteria);

            //Doesn't call search at all:
            //  XBox360 Video App

            //Calls search but does not require it to be implemented:
            //  WMP, probably uses it just for its "images" section

            //Calls search Seems to require it:
            //  XBox360 Music App

            //WMP
            //  action: "Search"
            //  object_id: "0"
            //  searchCriteria: "upnp:class derivedfrom \"object.item.imageItem\" and @refID exists false"
            //  filter: "*"
            //  starting_index: 0
            //  requested_count: 200
            //  sort_criteria: "-dc:date"

            //XBox360 Music App
            //  action: "Search"
            //  object_id: "7"
            //  searchCriteria: "(upnp:class = \"object.container.album.musicAlbum\")"
            //  filter: "dc:title,upnp:artist"
            //  starting_index: 0
            //  requested_count: 1000
            //  sort_criteria: "+dc:title"
            //
            //XBox360 Music App seems to work souly using SearchContainer and ProcessFileRequest
            //I think the current resource Uri's aren't going to work because it seems to require an extension like .mp3 to work, but this requires further testing
            //When hitting the Album tab of the app it's searching criteria is object.container.album.musicAlbum
            //this means it wants albums put into containers, I thought Platinum might do this for us, but it doesn't


            var didl = Platinum.Didl.header;
            int itemCount = 0;

            IEnumerable<Model.ModelBase> children = null;
            Model.ModelBase objectIDMatch;
            // I need to ask someone on the MB team if there's a better way to do this, it seems like it 
            //could get pretty expensive to get ALL children all the time
            //if it's our only option perhaps we should cache results locally or something similar
            var root = new Model.Root(this.CurrentUser);
            if (string.Equals(object_id, "0", StringComparison.OrdinalIgnoreCase))
                objectIDMatch = root;
            else
                objectIDMatch = root.GetChildRecursive(object_id);

            if (objectIDMatch == null)
            {
                didl += Platinum.Didl.footer;

                action.SetArgumentValue("Result", didl);
                action.SetArgumentValue("NumberReturned", itemCount.ToString());
                action.SetArgumentValue("TotalMatches", itemCount.ToString());

                // update ID may be wrong here, it should be the one of the container?
                action.SetArgumentValue("UpdateId", "1");

                return NEP_Success;
            }

            children = objectIDMatch.RecursiveChildren;


            if (children != null)
            {
                foreach (var child in children)
                {

                    using (var item = child.MediaObject)
                    {
                        if (item != null)
                        {
                            AddContextInfo(item, child.MainMediaResource, child.Id, child.Extension, context);

                            string test;
                            test = item.ToDidl(filter);
                            didl += item.ToDidl(filter);
                            itemCount++;
                        }
                    }
                }

                didl += Platinum.Didl.footer;

                action.SetArgumentValue("Result", didl);
                action.SetArgumentValue("NumberReturned", itemCount.ToString());
                action.SetArgumentValue("TotalMatches", itemCount.ToString());

                // update ID may be wrong here, it should be the one of the container?
                action.SetArgumentValue("UpdateId", "1");

                return NEP_Success;
            }
            
            return NEP_Failure;
        }

        private Platinum.MediaItem BaseItemToMediaItem(BaseItem child, Platinum.HttpRequestContext context)
        {
            Platinum.MediaItem result = null;
            Platinum.MediaResource resource = null;

            if (child.IsFolder)
            {
                //DLNA is a fairly flat system, there doesn't appear to be much room in the system for folders so far
                //I haven't tested too many DLNA clients yet tho
                result = null;
                //item =  new Platinum.MediaItem();
                //item.Class = new Platinum.ObjectClass("object.container.storageFolder", "");
            }
            else if (child is Episode)
            {
                result = MediaItemHelper.GetMediaItem((Episode)child);
                resource = MediaItemHelper.GetMediaResource((Episode)child);
            }
            else if (child is Video)
            {
                result = MediaItemHelper.GetMediaItem((Video)child);
                resource = MediaItemHelper.GetMediaResource((Video)child);
            }
            else if (child is Audio)
            {
                result = MediaItemHelper.GetMediaItem((Audio)child);
                resource = MediaItemHelper.GetMediaResource((Audio)child);
            }

            if (result != null)
            {
                //have a go at finding the mime type
                var mimeType = string.Empty;
                if (child.Path != null && Path.HasExtension(child.Path))
                    mimeType = MimeTypes.GetMimeType(child.Path);

                resource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfoFromMimeType(mimeType, true, context);

                // get list of ips and make sure the ip the request came from is used for the first resource returned
                // this ensures that clients which look only at the first resource will be able to reach the item
                IEnumerable<String> ips = GetUPnPIPAddresses(context); //.Distinct();

                // iterate through all ips and create a resource for each
                // I think we need extensions (".mp3" type extensions) on these for Xbox360 Video and Music apps to work


                //resource.URI = new Uri(Kernel.HttpServerUrlPrefix + "/api/video.ts?id=" + child.Id.ToString("D")).ToString();
                //result.AddResource(resource);

                foreach (String ip in ips)
                {
                    //doesn't work for WMP
                    //resource.URI = new Uri(Kernel.HttpServerUrlPrefix.Replace("+", ip) + "/api/video.ts?id=" + child.Id.ToString()).ToString();
                    resource.URI = new Uri("http://" + ip + ":" + context.LocalAddress.port + "/" + child.Id.ToString("D")).ToString();

                    result.AddResource(resource);
                }
                MediaItemHelper.AddAlbumArtInfoToMediaItem(result, child, Kernel.HttpServerUrlPrefix, ips);
            }

            return result;
        }

        private void AddContextInfo(Platinum.MediaObject result, Platinum.MediaResource resource, string id, string extension, Platinum.HttpRequestContext context)
        {
            if (result != null && resource != null)
            {
                //have a go at finding the mime type
                var mimeType = string.Empty;
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    mimeType = MimeTypes.GetMimeType(extension);
                    resource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfoFromMimeType(mimeType, true, context);
                }
                // get list of ips and make sure the ip the request came from is used for the first resource returned
                // this ensures that clients which look only at the first resource will be able to reach the item
                IEnumerable<String> ips = GetUPnPIPAddresses(context).Distinct();

                // iterate through all ips and create a resource for each
                // I think we need extensions (".mp3" type extensions) on these for Xbox360 Video and Music apps to work


                //resource.URI = new Uri(Kernel.HttpServerUrlPrefix + "/api/video.ts?id=" + child.Id.ToString("D")).ToString();
                //result.AddResource(resource);

                foreach (String ip in ips)
                {
                    //doesn't work for WMP
                    //resource.URI = new Uri(Kernel.HttpServerUrlPrefix.Replace("+", ip) + "/api/video.ts?id=" + child.Id.ToString()).ToString();
                    resource.URI = new Uri("http://" + ip + ":" + context.LocalAddress.port + "/" + id).ToString();

                    result.AddResource(resource);
                }
                
                MediaItemHelper.AddAlbumArtInfoToMediaItem(result, id, Kernel.HttpServerUrlPrefix, ips);
            }
        }

        private void AddResourcesToMediaItem(Platinum.MediaItem item, BaseItem child, Platinum.HttpRequestContext context)
        {
            Platinum.MediaResource resource = null;

            if (child is Video)
            {
                var videoChild = (Video)child;
                resource = new Platinum.MediaResource();

                if (videoChild.DefaultVideoStream != null)
                {
                    //Bitrate is Bytes per second
                    if (videoChild.DefaultVideoStream.BitRate.HasValue)
                        resource.Bitrate = (uint)videoChild.DefaultVideoStream.BitRate;

                    //not sure if we know Colour Depth
                    //resource.ColorDepth
                    if (videoChild.DefaultVideoStream.Channels.HasValue)
                        resource.NbAudioChannels = (uint)videoChild.DefaultVideoStream.Channels.Value;

                    //resource.Protection
                    //resource.ProtoInfo

                    //we must know resolution, I'm just not sure how to get it
                    //resource.Resolution

                    //I'm not sure what this actually means, is it Sample Rate
                    if (videoChild.DefaultVideoStream.SampleRate.HasValue)
                        resource.SampleFrequency = (uint)videoChild.DefaultVideoStream.SampleRate.Value;
                    //file size is bytes
                    //resource.Size

                }
            }
            else if (child is Audio)
            {

            }

            // get list of ips and make sure the ip the request came from is used for the first resource returned
            // this ensures that clients which look only at the first resource will be able to reach the item
            List<String> ips = GetUPnPIPAddresses(context);

            // iterate through all ips and create a resource for each
            // I think we need extensions (".mp3" type extensions) on these for Xbox360 Video and Music apps to work

            foreach (String ip in ips)
            {
                resource.URI = new Uri("http://" + ip + ":" + context.LocalAddress.port + "/" + child.Id.ToString("D")).ToString();
                item.AddResource(resource);
            }
        }

        /// <summary>
        /// Gets a list of valid IP Addresses that the UPnP server is using
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private List<String> GetUPnPIPAddresses(Platinum.HttpRequestContext context)
        {
            // get list of ips and make sure the ip the request came from is used for the first resource returned
            // this ensures that clients which look only at the first resource will be able to reach the item
            List<String> result = Platinum.UPnP.GetIpAddresses(true); //if this call is expensive we could cache the results
            String localIP = context.LocalAddress.ip;
            if (localIP != "0.0.0.0")
            {
                result.Remove(localIP);
                result.Insert(0, localIP);
            }
            return result;
        }

        /// <summary>
        /// Gets the MB User with a user name that matches the user name configured in the plugin config
        /// </summary>
        /// <returns>MediaBrowser.Controller.Entities.User</returns>
        private User CurrentUser
        {
            get
            {
                if (this._CurrentUser == null)
                {
                    //this looks like a lot of processing but it really isn't
                    //its mostly gaurding against no users or no matching user existing
                    var serverKernel = Controller.Kernel.Instance;
                    if (serverKernel.Users.Any())
                    {
                        if (string.IsNullOrWhiteSpace(this.Configuration.UserName))
                            this._CurrentUser = serverKernel.Users.First();
                        else
                        {
                            this._CurrentUser = serverKernel.Users.FirstOrDefault(i => string.Equals(i.Name, this.Configuration.UserName, StringComparison.OrdinalIgnoreCase));
                            if (this._CurrentUser == null)
                            {
                                //log and return first user
                                this._CurrentUser = serverKernel.Users.First();
                                Logger.Error("Configured user: \"{0}\" not found. Using first user found: \"{1}\" instead", this.Configuration.UserName, this._CurrentUser.Name);
                            }
                        }
                    }
                    else
                    {
                        Logger.Fatal("No users in the system");
                        this._CurrentUser = null;
                    }
                }
                return this._CurrentUser;
            }
        }

        #region "A Search Idea"
        //this is just an idea of how we might do some search
        //it's a bit lackluster in places and might be overkill in others
        //all in all it might not be a good idea, but I thought I'd see how it felt

        private Func<BaseItem, bool> GetBaseItemMatchFromCriteria(string searchCriteria)
        {
            //WMP Search when clicking Music:
            //"upnp:class derivedfrom \"object.item.audioItem\" and @refID exists false"
            //WMP Search when clicking Videos:
            //"upnp:class derivedfrom \"object.item.videoItem\" and @refID exists false"
            //WMP Search when clicking Pictures:
            //"upnp:class derivedfrom \"object.item.imageItem\" and @refID exists false"
            //WMP Search when clicking Recorded TV:
            //"upnp:class derivedfrom \"object.item.videoItem\" and @refID exists false"

            //we really need a syntax tree parser here
            //but the requests never seem to get more complex than "'Condition One' And 'Condition Two'"
            //something like Rosylin would be fun but it'd be serious overkill
            //the syntax seems to be very clear and there are only a handful of valid constructs
            //so this very basic parsing will provide some support for now

            Queue<string> criteriaQueue = new Queue<string>(searchCriteria.Split(' '));

            Func<BaseItem, bool> result = null;
            var currentMainOperatorIsAnd = false;

            //loop through in order and process - do not parallelise, order is important
            while (criteriaQueue.Any())
            {
                Func<BaseItem, bool> currentFilter = null;

                var metadataElement = criteriaQueue.Dequeue();
                var criteriaOperator = criteriaQueue.Dequeue();
                var value = criteriaQueue.Dequeue();
                if (value.StartsWith("\"") || value.StartsWith("\\\""))
                    while (!value.EndsWith("\""))
                    {
                        value += criteriaQueue.Dequeue();
                    }
                value = value.Trim();


                if (string.Equals(metadataElement, "upnp:class", StringComparison.OrdinalIgnoreCase))
                    currentFilter = GetUpnpClassFilter(criteriaOperator, value);
                else if (string.Equals(metadataElement, "@refID", StringComparison.OrdinalIgnoreCase))
                {
                    //not entirely sure what refID is for
                    //Platinum has ReferenceID which I assume is the same thing, but we're not using it yet

                }
                else
                {
                    //fail??
                }


                if (currentFilter != null)
                {
                    if (result == null)
                        result = currentFilter;
                    else
                        if (currentMainOperatorIsAnd)
                            result = (i) => result(i) && currentFilter(i);
                        else
                            result = (i) => result(i) || currentFilter(i);
                }
                if (criteriaQueue.Any())
                {
                    var op = criteriaQueue.Dequeue();
                    if (string.Equals(op, "and", StringComparison.OrdinalIgnoreCase))
                        currentMainOperatorIsAnd = true;
                    else
                        currentMainOperatorIsAnd = false;
                }
            }
            return result;
        }

        private Func<BaseItem, bool> GetUpnpClassFilter(string criteriaOperator, string value)
        {
            //"upnp:class derivedfrom \"object.item.videoItem\" "
            //"(upnp:class = \"object.container.album.musicAlbum\")"

            //only two options are valid for criteria
            // =, derivedfrom

            //there are only a few values we care about
            //object.item.videoItem
            //object.item.audioItem
            //object.container.storageFolder

            if (string.Equals(criteriaOperator, "=", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Contains("object.item.videoItem"))
                    return (i) => (i is Video);
                else if (value.Contains("object.item.audioItem"))
                    return (i) => (i is Audio);
                else if (value.Contains("object.container.storageFolder"))
                    return (i) => (i is Folder);
                else
                    //something has gone wrong, don't filter anything
                    return (i) => true;
            }
            else if (string.Equals(criteriaOperator, "derivedfrom", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Contains("object.item.videoItem"))
                    return (i) => (i is Video);
                else if (value.Contains("object.item.audioItem"))
                    return (i) => (i is Audio);
                else if (value.Contains("object.container.storageFolder"))
                    return (i) => (i is Folder);
                else
                    //something has gone wrong, don't filter anything
                    return (i) => true;
            }
            else
            {
                //something has gone wrong, don't filter anything
                return (i) => true;
            }
        }
        #endregion

        public override void UpdateConfiguration(MediaBrowser.Model.Plugins.BasePluginConfiguration configuration)
        {
            base.UpdateConfiguration(configuration);
            var config = (PluginConfiguration)configuration;

            this.CleanupUPnPServer();
            this._CurrentUser = null;
            this.SetupUPnPServer();
        }
    }

    internal static class MediaItemHelper
    {
        internal static Platinum.MediaResource GetMediaResource(Video item)
        {
            var result = GetMediaResource((BaseItem)item);

            if (item.DefaultVideoStream != null)
            {
                //Bitrate - The bitrate in bytes/second of the resource.
                if (item.DefaultVideoStream.BitRate.HasValue)
                    result.Bitrate = (uint)item.DefaultVideoStream.BitRate;

                //ColourDepth - The color depth in bits of the resource (image or video).
                //result.ColorDepth

                //NbAudioChannels - Number of audio channels of the resource, e.g. 1 for mono, 2 for stereo, 6 for Dolby surround, etc.
                if (item.DefaultVideoStream.Channels.HasValue)
                    result.NbAudioChannels = (uint)item.DefaultVideoStream.Channels.Value;

                //Protection - Some statement of the protection type of the resource (not standardized).
                //result.Protection

                //ProtoInfo - The 'protocolInfo' attribute is a string that identifies the
                //streaming or transport protocol for transmitting the resource.
                //If not present then the content has not yet been fully imported by
                //the ContentDirectory service and is not yet accessible for playback.
                //result.ProtoInfo

                //Resolution - X*Y resolution of the resource (image or video).
                //The string pattern is restricted to strings of the form:
                //[0-9]+x[0-9]+
                //(one or more digits,'x', followed by one or more digits).
                //SampleFrequency - The sample frequency of the resource in Hz
                //result.Resolution

                if (item.DefaultVideoStream.SampleRate.HasValue)
                    result.SampleFrequency = (uint)item.DefaultVideoStream.SampleRate.Value;

                //Size - size, in bytes, of the resource.
                //result.Size


                ////to do subtitles for clients that can deal with external subtitles (like srt)
                ////we will have to do something like this
                //IEnumerable<String> ips = GetUPnPIPAddresses(context);
                //foreach (var st in videoChild.MediaStreams)
                //{
                //    if (st.Type == MediaStreamType.Subtitle)
                //    {
                //        Platinum.MediaResource subtitleResource = new Platinum.MediaResource();
                //        subtitleResource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfo(st.Path, with_dlna_extension: false);
                //        foreach (String ip in ips)
                //        {
                //            //we'll need to figure out which of these options works for whick players
                //            //either serve them ourselves
                //            resource.URI = new Uri("http://" + ip + ":" + context.LocalAddress.port + "/" + child.Id.ToString("D")).ToString();
                //            //or get the web api to serve them directly
                //            resource.URI = new Uri(Kernel.HttpServerUrlPrefix.Replace("+", ip) + "/api/video?id=" + child.Id.ToString() + "&type=Subtitle").ToString();
                //            result.AddResource(resource);
                //        }
                //    }
                //}
            }
            return result;
        }
        internal static Platinum.MediaItem GetMediaItem(Video item)
        {
            var result = GetMediaItem((BaseItem)item);
            result.Title = GetTitle(item);
            
            return result;
        }

        internal static Platinum.MediaResource GetMediaResource(Episode item)
        {
            //there's nothing specific about an episode that requires extra Resources
            return GetMediaResource((Video)item);
        }
        internal static Platinum.MediaItem GetMediaItem(Episode item)
        {
            var result = GetMediaItem((Video)item);

            if (item.IndexNumber.HasValue)
                result.Recorded.EpisodeNumber = (uint)item.IndexNumber.Value;
            if (item.Series != null && item.Series.Name != null)
                result.Recorded.SeriesTitle = item.Series.Name;
            result.Recorded.ProgramTitle = item.Name == null ? string.Empty : item.Name;

            return result;
        }

        internal static Platinum.MediaResource GetMediaResource(Audio item)
        {
            //there's nothing specific about an audio item that requires extra Resources
            return GetMediaResource((BaseItem)item);
        }
        internal static Platinum.MediaItem GetMediaItem(Audio item)
        {
            var result = GetMediaItem((BaseItem)item);
            result.Title = GetTitle(item);
            result.People.AddArtist(new Platinum.PersonRole(item.Artist));
            result.People.Contributor = item.AlbumArtist;
            result.Affiliation.Album = item.Album;
            return result;
        }

        internal static Platinum.MediaResource GetMediaResource(MusicArtist item)
        {
            //there's nothing specific about an music artist item that requires extra Resources
            return GetMediaResource((BaseItem)item);
        }
        internal static Platinum.MediaItem GetMediaItem(MusicArtist item)
        {
            var result = GetMediaItem((BaseItem)item);
            result.Title = item.Name; 
            return result;
        }

        internal static Platinum.MediaResource GetMediaResource(MusicAlbum item)
        {
            //there's nothing specific about an music artist item that requires extra Resources
            return GetMediaResource((BaseItem)item);
        }
        internal static Platinum.MediaItem GetMediaItem(MusicAlbum item)
        {
            var result = GetMediaItem((BaseItem)item);
            result.Title = item.Name;
            return result;
        }

        internal static Platinum.MediaResource GetMediaResource(BaseItem item)
        {
            var result = new Platinum.MediaResource();

            //duration - The 'duration' attribute identifies the duration of the playback of
            //the resource, at normal speed.
            //The format of the duration string is:
            //H+:MM:SS[.F+], or H+:MM:SS[.F0/F1]
            //Where:
            //+H		one or more digits to indicate elapsed hours,
            //MM		exactly 2 digits to indicate minutes (00 to 59),
            //SS		exactly 2 digits to indicate seconds (00 to 59),
            //F+		any number of digits (including no digits) to indicate fractions of seconds,
            //F0/F1	a fraction, with F0 and F1 at least one digit long,
            //        and F0 < F1.
            //The string may be preceded by an optional + or - sign, and the
            //decimal point itself may be omitted if there are no fractional	seconds digits.            
            
            //we don't have to worry about the string formating because Platinum does it for us
            //we just have to give it the duration in seconds
            if (item.RunTimeTicks.HasValue)
                result.Duration = (uint)TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

            return result;
        }
        internal static Platinum.MediaItem GetMediaItem(BaseItem item)
        {
            var result = new Platinum.MediaItem();

            result.ObjectID = item.Id.ToString();

            //if (child.Parent != null)
            //    result.ParentID = child.Parent.Id.ToString();
            result.Class = item.GetPlatinumClassObject();

            result.Description.Date = item.PremiereDate.HasValue ? item.PremiereDate.Value.ToString() : string.Empty;
            result.Description.Language = item.Language == null ? string.Empty : item.Language;
            result.Description.DescriptionText = "this is DescriptionText";
            result.Description.LongDescriptionText = item.Overview == null ? string.Empty : item.Overview;
            result.Description.Rating = item.CommunityRating.ToString();
            
            if (item.Genres != null)
            {
                foreach (var genre in item.Genres)
                {
                    result.Affiliation.AddGenre(genre);
                }
            }
            if (item.People != null)
            {
                foreach (var person in item.People)
                {
                    if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                        result.People.AddActor(new Platinum.PersonRole(person.Name, person.Role == null ? string.Empty : person.Role));
                    else if (string.Equals(person.Type, PersonType.MusicArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "MusicArtist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "artist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "Performer"));
                    }
                    else if (string.Equals(person.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Composer"));
                        result.Creator = person.Name;
                    }
                    else if (string.Equals(person.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Writer"));
                    else if (string.Equals(person.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Director"));
                        result.People.Director = result.People.Director + " " + person.Name;
                    }
                    else
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, person.Type == null ? string.Empty : person.Type));
                }
            }

            //'restricted' attribute (true, false, 1, 0).
            //When restricted="true", the ability to change or delete the
            //Container or Item is restricted.            
            //result.Restricted

            return result;
        }

        internal static void AddAlbumArtInfoToMediaItem(Platinum.MediaObject item, BaseItem child, string httpServerUrlPrefix, IEnumerable<String> ips)
        {
            foreach (var ip in ips)
            {
                AddAlbumArtInfoToMediaItem(item, child, httpServerUrlPrefix, ip);
            }
        }
        private static void AddAlbumArtInfoToMediaItem(Platinum.MediaObject item, BaseItem child, string httpServerUrlPrefix, string ip)
        {
            if (item == null || child == null) return;

            //making the artwork a direct hit to the MediaBrowser server instead of via the DLNA plugin works for WMP
            item.Extra.AddAlbumArtInfo(new Platinum.AlbumArtInfo(httpServerUrlPrefix.Replace("+", ip) + "/api/image?id=" + child.Id.ToString() + "&type=primary"));
        }
        internal static void AddAlbumArtInfoToMediaItem(Platinum.MediaObject item, string id, string httpServerUrlPrefix, IEnumerable<String> ips)
        {
            foreach (var ip in ips)
            {
                AddAlbumArtInfoToMediaItem(item, id, httpServerUrlPrefix, ip);
            }
        }
        private static void AddAlbumArtInfoToMediaItem(Platinum.MediaObject item, string id, string httpServerUrlPrefix, string ip)
        {
            if (item == null) return;

            //making the artwork a direct hit to the MediaBrowser server instead of via the DLNA plugin works for WMP
            item.Extra.AddAlbumArtInfo(new Platinum.AlbumArtInfo(httpServerUrlPrefix.Replace("+", ip) + "/api/image?id=" + id + "&type=primary"));
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <returns>System.String.</returns>
        private static string GetTitle(Video video)
        {
            //we have to be extremely careful with all string handling
            //if we set a null reference to a Platinum string it will not marshall to native correctly and things got very bad very quickly
            var title = video.Name == null ? string.Empty : video.Name;

            var episode = video as Episode;

            if (episode != null)
            {
                if (episode.Season != null)
                {
                    title = string.Format("{0}-{1}", episode.Season.Name, title);
                }
                if (episode.Series != null)
                {
                    title = string.Format("{0}-{1}", episode.Series.Name, title);
                }
            }

            return title;
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <returns>System.String.</returns>
        private static string GetTitle(Audio audio)
        {
            return audio.Name == null ? string.Empty : audio.Name;
        }

    }

    internal static class Extensions
    {
        [Flags()]
        internal enum FilterType
        {
            Folder = 1,
            Music = 2,
            Video = 4
        }
        internal static IEnumerable<BaseItem> Filter(this IEnumerable<BaseItem> en, FilterType filter)
        {
            return en.Where(i => (
                (((filter & FilterType.Folder) == FilterType.Folder) && (i is Folder)) ||
                (((filter & FilterType.Music) == FilterType.Music) && (i is Audio)) ||
                (((filter & FilterType.Video) == FilterType.Video) && (i is Video)))
                                );
        }

        internal static IEnumerable<BaseItem> Page(this IEnumerable<BaseItem> en, int starting_index, int requested_count)
        {
            return en.Skip(starting_index).Take(requested_count);
        }


        internal static Platinum.ObjectClass GetPlatinumClassObject(this BaseItem item)
        {
            if (item is Video)
                return new Platinum.ObjectClass("object.item.videoItem", "");
            else if (item is Audio)
                return new Platinum.ObjectClass("object.item.audioItem.musicTrack", "");
            else if (item is Folder)
                return new Platinum.ObjectClass("object.container.storageFolder", "");
            else
                return null;
        }
        internal static Platinum.ObjectClass GetPlatinumClassObject(this Folder item)
        {
            return new Platinum.ObjectClass("object.container.storageFolder", "");
        }
        internal static Platinum.ObjectClass GetPlatinumClassObject(this Audio item)
        {
            return new Platinum.ObjectClass("object.item.audioItem.musicTrack", "");
        }
        internal static Platinum.ObjectClass GetPlatinumClassObject(this Video item)
        {
            return new Platinum.ObjectClass("object.item.videoItem", "");
        }
    }
    internal static class LoggingExtensions
    {
        //provide some json-esque string that can be used for Verbose logging purposed
        internal static string ToLogString(this Platinum.Action item)
        {
            return string.Format(" {{ Name:\"{0}\", Description:\"{1}\", Arguments:{2} }} ",
                item.Name, item.Description.ToLogString(), item.Arguments.ToLogString());

        }
        internal static string ToLogString(this IEnumerable<Platinum.ActionArgumentDescription> items)
        {
            var result = "[";
            foreach (var arg in items)
            {
                result += (" " + arg.ToLogString());
            }
            result += " ]";
            return result;
        }
        internal static string ToLogString(this Platinum.ActionArgumentDescription item)
        {
            return string.Format(" {{ Name:\"{0}\", Direction:{1}, HasReturnValue:{2}, RelatedStateVariable:{3} }} ",
                item.Name, item.Direction, item.HasReturnValue, item.RelatedStateVariable.ToLogString());

        }
        internal static string ToLogString(this Platinum.StateVariable item)
        {
            return string.Format(" {{ Name:\"{0}\", DataType:{1}, DataTypeString:\"{2}\", Value:{3}, ValueString:\"{4}\" }} ",
                item.Name, item.DataType, item.DataTypeString, item.Value, item.ValueString);
        }
        internal static string ToLogString(this Platinum.ActionDescription item)
        {
            return string.Format(" {{ Name:\"{0}\", Arguments:{1} }} ",
                item.Name, item.Arguments.ToLogString());
        }
        internal static string ToLogString(this Platinum.HttpRequestContext item)
        {
            return string.Format(" {{ LocalAddress:{0}, RemoteAddress:{1}, Request:\"{2}\", Signature:{3} }}",
                item.LocalAddress.ToLogString(), item.RemoteAddress.ToLogString(), item.Request.URI.ToString(), item.Signature);
        }
        internal static string ToLogString(this Platinum.HttpRequestContext.SocketAddress item)
        {
            return string.Format("{{ IP:{0}, Port:{1} }}",
                item.ip, item.port);
        }
    }
}
