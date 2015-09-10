namespace OneDrive
{
    internal class BufferArray
    {
        private readonly byte[] _buffer;

        public BufferArray()
            : this(new byte[0], 0)
        { }

        public BufferArray(byte[] buffer, int size)
        {
            _buffer = buffer;
            Count = size;
        }

        public int Count { get; private set; }

        public byte[] Array
        {
            get
            {
                var array = new byte[Count];
                System.Array.Copy(_buffer, array, Count);
                return array;
            }
        }
    }
}
