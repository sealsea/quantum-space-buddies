using System;
using System.Text;

namespace EpicTransport
{
	public static class RandomString
	{
		// Generates a random string with a given size.
		public static string Generate(int size)
		{
			var builder = new StringBuilder(size);

			var random = new Random();

			// Unicode/ASCII Letters are divided into two blocks
			// (Letters 65–90 / 97–122):
			// The first group containing the uppercase letters and
			// the second group containing the lowercase.

			// char is a single Unicode character
			const char offsetLowerCase = 'a';
			const char offsetUpperCase = 'A';
			const int lettersOffset = 26; // A...Z or a..z: length=26

			for (var i = 0; i < size; i++)
			{
				var offset = random.Next(0, 2) == 0 ? offsetLowerCase : offsetUpperCase;

				var @char = (char)random.Next(offset, offset + lettersOffset);
				builder.Append(@char);
			}

			return builder.ToString();
		}
	}
}
