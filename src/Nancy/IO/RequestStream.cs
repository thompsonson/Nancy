namespace Nancy.IO
{
    using System;
    using System.IO;

    /// <summary>
    /// A <see cref="Stream"/> decorator that can handle moving the stream out from memory and on to disk when the contents reaches a certain length.
    /// </summary>
    public class RequestStream : Stream
    {
        private bool disableStreamSwitching;
        private readonly long expectedLength;
        private readonly long thresholdLength;
        private Stream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestStream"/> class.
        /// </summary>
        /// <param name="expectedLength">The expected length of the contents in the stream.</param>
        /// <param name="thresholdLength">The content length that will trigger the stream to be moved out of memory.</param>
        /// <param name="disableStreamSwitching">if set to <see langword="true"/> the stream will never explicitly be moved to disk.</param>
        public RequestStream(long expectedLength, long thresholdLength, bool disableStreamSwitching)
            : this(null, expectedLength, thresholdLength, disableStreamSwitching)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestStream"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> that should be handled by the request stream</param>
        /// <param name="expectedLength">The expected length of the contents in the stream.</param>
        /// <param name="thresholdLength">The content length that will trigger the stream to be moved out of memory.</param>
        /// <param name="disableStreamSwitching">if set to <see langword="true"/> the stream will never explicitly be moved to disk.</param>
        public RequestStream(Stream stream, long expectedLength, long thresholdLength, bool disableStreamSwitching)
        {
            this.expectedLength = expectedLength;
            this.thresholdLength = thresholdLength;
            this.disableStreamSwitching = disableStreamSwitching;
            this.stream = stream ?? this.CreateDefaultMemoryStream();

            if (!this.stream.CanSeek)
                throw new InvalidOperationException("The stream must support seeking.");

            if (!this.stream.CanRead)
                throw new InvalidOperationException("The stream must support reading.");

            if (expectedLength < 0)
                throw new ArgumentOutOfRangeException("expectedLength", expectedLength, "The value of the expectedLength parameter cannot be less than zero.");

            if (thresholdLength < 0)
                throw new ArgumentOutOfRangeException("thresholdLength", thresholdLength, "The value of the threshHoldLength parameter cannot be less than zero.");

            this.stream.Position = 0;

            if (this.expectedLength >= this.thresholdLength)
            {
                this.MoveStreamContentsToFileStream();
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        /// <returns>Always returns <see langword="false"/>.</returns>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns>Always returns <see langword="false"/>.</returns>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get { return this.stream.Length; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream is stored in memory.
        /// </summary>
        /// <value><see langword="true"/> if the stream is stored in memory; otherwise, <see langword="false"/>.</value>
        /// <remarks>The stream is moved to disk when either the length of the contents or expected content length exceeds the threshold specified in the constructor.</remarks>
        public bool IsInMemory
        {
            get { return !this.stream.GetType().Equals(typeof(FileStream)); }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get { return this.stream.Position; }
            set
            {
                if (value < 0)
                    throw new InvalidOperationException("The position of the stream cannot be set to less than zero.");

                if (value > this.Length)
                    throw new InvalidOperationException("The position of the stream cannot exceed the length of the stream.");

                this.stream.Position = value;
            }
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// </summary>
        public override void Close()
        {
            this.stream.Close();
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            this.stream.Flush();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream. </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream. </param>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            return this.stream.ReadByte();
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <returns>The new position within the current stream.</returns>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter. </param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position. </param>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes. </param>
        /// <exception cref="NotSupportedException">The stream does not support having it's length set.</exception>
        /// <remarks>This functionalitry is not supported by the <see cref="RequestStream"/> type and will always throw <see cref="NotSupportedException"/>.</remarks>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.stream.Write(buffer, offset, count);

            if (this.disableStreamSwitching)
            {
                return;
            }

            if (this.stream.Length >= this.thresholdLength)
            {
                this.MoveStreamContentsToFileStream();
            }
        }

        private static FileStream CreateTemporaryFileStream()
        {
            var fileName = Path.GetTempFileName();
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            return new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, true);
        }

        private Stream CreateDefaultMemoryStream()
        {
            if (this.disableStreamSwitching || this.expectedLength < this.thresholdLength)
            {
                return new MemoryStream((int)this.expectedLength);
            }

            this.disableStreamSwitching = true;

            return CreateTemporaryFileStream();
        }

        private void MoveStreamContentsToFileStream()
        {
            if (this.disableStreamSwitching)
            {
                return;
            }

            var fileStream =
                CreateTemporaryFileStream();

            if (this.stream.Length == 0)
            {
                this.stream.Close();
                this.stream = fileStream;
                return;
            }

            this.stream.Position = 0;
            this.stream.CopyTo(fileStream, 8196);
            this.stream.Flush();

            fileStream.Position = 0;
            this.stream = fileStream;
        }
    }
}