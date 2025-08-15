using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace Clay_cs;

public static class Utils
{
	public static unsafe SpanOwner<sbyte> ToSpanOwner(this Clay_String text)
	{
		var span = SpanOwner<sbyte>.Empty;
		if (text.length > 0)
		{
			span = SpanOwner<sbyte>.Allocate(text.length + 1, AllocationMode.Default);
			for (int j = 0; j < text.length; j++) span.Span[j] = text.chars[j];
			span.Span[^1] = (sbyte)'\0';
		}

		return span;
	}

	public static unsafe SpanOwner<sbyte> ToSpanOwner(this Clay_StringSlice text)
	{
		var span = SpanOwner<sbyte>.Empty;
		if (text.length > 0)
		{
			span = SpanOwner<sbyte>.Allocate(text.length + 1, AllocationMode.Default);
			for (int j = 0; j < text.length; j++) span.Span[j] = text.chars[j];
			span.Span[^1] = (sbyte)'\0';
		}
	
		return span;
	}
	
	public static unsafe string ToCSharpString(this Clay_StringSlice text)
	{
		if (text.length == 0) return string.Empty;
	
		Span<byte> span = stackalloc byte[text.length];
		for (int j = 0; j < text.length; j++)
		{
			span[j] = unchecked((byte)text.chars[j]);
		}
	
		return Encoding.UTF8.GetString(span);
	}
	
	public static unsafe string ToCSharpString(this Clay_String text)
	{
		if (text.length == 0) return string.Empty;

		Span<byte> span = stackalloc byte[text.length];
		for (int j = 0; j < text.length; j++)
		{
			span[j] = unchecked((byte)text.chars[j]);
		}

		return Encoding.UTF8.GetString(span);
	}
}