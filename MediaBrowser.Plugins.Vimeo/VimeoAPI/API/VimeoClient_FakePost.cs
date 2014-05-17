#if WINDOWS
using System.Web;
#endif
#if VFW
using VFW2;
using System.Windows.Forms;
#endif

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.API
{
#if WINDOWS && !UPLOAD
    public partial class VimeoClient
    {
        void painInTheAss()
        {
            Debug.Fail("This version of VimeoDotNet does not include upload support.\n" +
                "Please consider donating to the project in order to get the\n" +
                "full version with advanced upload support. Thank you!\n" +
                "Visit vimeodotnet.saeedoo.com for more information.");
        }

        public int GetChunksCount(string path, int chunk_size = 1048576)
        {
            painInTheAss();
            return 0;
        }

        public int GetChunksCount(long fileSize, int chunk_size = 1048576)
        {
            painInTheAss();
            return 0;
        }

        public int GetChunkSize(string path, int index, int size = 1048576)
        {
            painInTheAss();
            return 0;
        }

        public int GetChunkSize(long fileSize, int index, int size = 1048576)
        {
            painInTheAss();
            return 0;
        }

        public string Upload(string path)
        {
            painInTheAss();
            return "version does not support upload";
        }

        public string ResumeUploadInChunks(string path, Ticket t, int chunk_size = 1048576, int max_chunks = -1)
        {
            painInTheAss();
            return "version does not support upload";
        }

        string ResumeUploadInChunks(string path, Ticket t, int chunk_size, int max_chunks, bool firstTime)
        {
            painInTheAss();
            return "version does not support upload";
        }

        public string UploadInChunks(string path, int chunk_size = 1048576)
        {
            painInTheAss();
            return "version does not support upload";
        }

        public string UploadInChunks(string path, out Ticket ticket, int chunk_size = 1048576, int max_chunks = -1)
        {

            painInTheAss();
            ticket = null;
            return "version does not support upload";
        }

        public string PostVideo(Ticket ticket, string path)
        {
            return PostVideo(ticket, 0, path, 0, -1);
        }

        public string PostVideo(Ticket ticket, int index, string path, int chunk_size = 1048576)
        {
            return PostVideo(ticket, index, path, index * chunk_size, chunk_size);
        }

        public string PostVideo(Ticket ticket, int chunk_id, string path, long startbyte, int? psize)
        {
            painInTheAss();
            return "version does not support upload";
        }

        public bool UseProxyForPost = true;

        public string PostVideo(Ticket ticket, int chunk_id, string file_name, byte[] file_data)
        {
            painInTheAss();
            return "version does not support upload";
        }

    }
#endif
}