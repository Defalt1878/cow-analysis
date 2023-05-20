namespace Core;

public static class StringExtensions
{
	public static string MaskAsSecret(this string str)
	{
		if (str.Length < 5)
			return string.Join("", Enumerable.Repeat("*", str.Length));

		return str[..2] + string.Join("", Enumerable.Repeat("*", str.Length - 4)) + str[^2..];
	}
}