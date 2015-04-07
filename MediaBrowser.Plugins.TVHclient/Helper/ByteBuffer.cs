
namespace MediaBrowser.Plugins.TVHclient.Helper
{
    public class ByteBuffer
    {
        private System.IO.MemoryStream stream;
        private System.IO.BinaryReader reader;
        private System.IO.BinaryWriter writer;

        public ByteBuffer(byte[] data)
        {
            stream = new System.IO.MemoryStream();
            reader = new System.IO.BinaryReader(stream);
            writer = new System.IO.BinaryWriter(stream);
            writer.Write(data);
            stream.Position = 0;
        }

        ~ByteBuffer()
        {
            reader.Close();
            writer.Close();
            stream.Close();
            stream.Dispose();
        }

        public long Length()
        {
            return stream.Length;
        }

        public bool hasRemaining()
        {
            return (stream.Length - stream.Position) > 0;
        }

        public byte get()
        {
            return (byte)stream.ReadByte();
        }

        public void get(byte[] dst)
        {
            stream.Read(dst, 0, dst.Length);
        }
    }
}
