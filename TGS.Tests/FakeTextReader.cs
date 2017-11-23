using System.IO;
using System.Threading.Tasks;

namespace TGS.TestHelpers
{
	/// <summary>
	/// Fake implementation of <see cref="TextReader"/>
	/// </summary>
	sealed class FakeTextReader : TextReader
	{
		public override void Close() { }
		public override int Peek() { return 1; }
		public override int Read() { return 65; }
		public override int Read(char[] buffer, int index, int count)
		{
			if (count < 1)
				return 0;
			buffer[0] = 'A';
			return 1;
		}
		public override Task<int> ReadAsync(char[] buffer, int index, int count)
		{
			return Task.Factory.StartNew(() => Read());
		}
		public override int ReadBlock(char[] buffer, int index, int count)
		{
			return Read(buffer, index, count);
		}
		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
		{
			return ReadAsync(buffer, index, count);
		}
		public override string ReadLine()
		{
			return "asdf";
		}
		public override Task<string> ReadLineAsync()
		{
			return Task.Factory.StartNew(() => ReadLine());
		}
		public override string ReadToEnd()
		{
			return ReadLine();
		}
		public override Task<string> ReadToEndAsync()
		{
			return ReadLineAsync();
		}
	}
}
