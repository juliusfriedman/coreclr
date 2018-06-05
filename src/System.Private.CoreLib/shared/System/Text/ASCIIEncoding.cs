// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Text
{
    // ASCIIEncoding
    //
    // Note that ASCIIEncoding is optimized with no best fit and ? for fallback.
    // It doesn't come in other flavors.
    //
    // Note: ASCIIEncoding is the only encoding that doesn't do best fit (windows has best fit).
    //
    // Note: IsAlwaysNormalized remains false because 1/2 the code points are unassigned, so they'd
    //       use fallbacks, and we cannot guarantee that fallbacks are normalized.

    public class ASCIIEncoding : Encoding
    {
        // Allow for devirtualization (see https://github.com/dotnet/coreclr/pull/9230)
        internal sealed class ASCIIEncodingSealed : ASCIIEncoding { }

        // Used by Encoding.ASCII for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly ASCIIEncodingSealed s_default = new ASCIIEncodingSealed();

        public ASCIIEncoding() : base(Encoding.CodePageASCII)
        {
        }

        internal override void SetDefaultFallbacks()
        {
            // For ASCIIEncoding we just use default replacement fallback
            this.encoderFallback = EncoderFallback.ReplacementFallback;
            this.decoderFallback = DecoderFallback.ReplacementFallback;
        }

        // WARNING: GetByteCount(string chars), GetBytes(string chars,...), and GetString(byte[] byteIndex...)
        // WARNING: have different variable names than EncodingNLS.cs, so this can't just be cut & pasted,
        // WARNING: or it'll break VB's way of calling these.
        //
        // The following methods are copied from EncodingNLS.cs.
        // Unfortunately EncodingNLS.cs is internal and we're public, so we have to re-implement them here.
        // These should be kept in sync for the following classes:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            // Validate input parameters
            if (null == chars)
                throw new ArgumentNullException(nameof(chars), SR.ArgumentNull_Array);

            if (index < 0 | count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input, return 0, avoid fixed empty array problem
            if (count == 0)
                return 0;

            // Just call the pointer version
            fixed (char* pChars = chars)
                return GetByteCount(pChars + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(String chars)
        {
            // Validate input
            if (null == chars)
                throw new ArgumentNullException(nameof(chars));

            fixed (char* pChars = chars)
                return GetByteCount(pChars, chars.Length, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetByteCount(char* chars, int count)
        {
            // Validate Parameters
            if (null == chars)
                throw new ArgumentNullException(nameof(chars), SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Call it with empty encoder
            return GetByteCount(chars, count, null);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        public override unsafe int GetBytes(String chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            if (null == chars | null == bytes)
                throw new ArgumentNullException((null == chars ? nameof(chars) : nameof(bytes)), SR.ArgumentNull_Array);

            if (charIndex < 0 | charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCount);

            if (byteIndex < 0 | byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_Index);

            int byteCount = bytes.Length - byteIndex;

            fixed (char* pChars = chars) fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            // Validate parameters
            if (null == chars | null == bytes)
                throw new ArgumentNullException((null == chars ? nameof(chars) : nameof(bytes)), SR.ArgumentNull_Array);

            if (charIndex < 0 | charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 | byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_Index);

            // If nothing to encode return 0
            if (charCount == 0)
                return 0;

            // Just call pointer version
            int byteCount = bytes.Length - byteIndex;

            fixed (char* pChars = chars)  fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                // Remember that byteCount is # to decode, not size of array.
                return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            // Validate Parameters
            if (null == bytes | null == chars)
                throw new ArgumentNullException(null == bytes ? nameof(bytes) : nameof(chars), SR.ArgumentNull_Array);

            if (charCount < 0 | byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetBytes(chars, charCount, bytes, byteCount, null);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            // Validate Parameters
            if (null == bytes)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (index < 0 | count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input just return 0, fixed doesn't like 0 length arrays
            if (count == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = bytes)
                return GetCharCount(pBytes + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate Parameters
            if (null == bytes)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetCharCount(bytes, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            // Validate Parameters
            if (null == bytes | null == chars)
                throw new ArgumentNullException(null == bytes ? nameof(bytes) : nameof(chars), SR.ArgumentNull_Array);

            if (byteIndex < 0 | byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 | charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_Index);

            // If no input, return 0 & avoid fixed problem
            if (byteCount == 0)
                return 0;

            // Just call pointer version
            int charCount = chars.Length - charIndex;

            fixed (byte* pBytes = bytes) fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                // Remember that charCount is # to decode, not size of array
                return GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            // Validate Parameters
            if (null == bytes | null == chars)
                throw new ArgumentNullException(null == bytes ? nameof(bytes) : nameof(chars), SR.ArgumentNull_Array);

            if (charCount < 0 | byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe string GetString(byte[] bytes, int byteIndex, int byteCount)
        {
            // Validate Parameters
            if (null == bytes)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (byteIndex < 0 | byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);


            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid problems with empty input buffer
            if (byteCount == 0) return string.Empty;

            fixed (byte* pBytes = bytes)
                return string.CreateStringFromEncoding(
                    pBytes + byteIndex, byteCount, this);
        }

        //
        // End of standard methods copied from EncodingNLS.cs
        //

        // GetByteCount
        // Note: We start by assuming that the output will be the same as count.  Having
        // an encoder or fallback may change that assumption
        // Optimized by TryEncodeAsciiCharsToBytes
        internal override unsafe int GetByteCount(char* chars, int charCount, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(charCount >= 0, "[ASCIIEncoding.GetByteCount]count is negative");
            Debug.Assert(chars != null, "[ASCIIEncoding.GetByteCount]chars is null");

            // Assert because we shouldn't be able to have a null encoder.
            Debug.Assert(encoderFallback != null, "[ASCIIEncoding.GetByteCount]Attempting to use null fallback encoder");

            char charLeftOver = default;
            EncoderReplacementFallback fallback = null;

            // Start by assuming default count, then +/- for fallback characters
            char* charEnd = chars + charCount;

            // For fallback we may need a fallback buffer, we know we aren't default fallback.
            EncoderFallbackBuffer fallbackBuffer = null;
            char* charsForFallback;

            if (encoder != null)
            {
                charLeftOver = encoder._charLeftOver;
                Debug.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[ASCIIEncoding.GetByteCount]leftover character should be high surrogate");

                fallback = encoder.Fallback as EncoderReplacementFallback;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder._throwOnOverflow)
                        throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(chars, charEnd, encoder, false);
                }

                // Verify that we have no fallbackbuffer, for ASCII its always empty, so just assert
                Debug.Assert(!encoder._throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetByteCount]Expected empty fallback buffer");
            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            // If we have an encoder AND we aren't using default fallback,
            // then we may have a complicated count.
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Replacement fallback encodes surrogate pairs as two ?? (or two whatever), so return size is always
                // same as input size.
                // Note that no existing SBCS code pages map code points to supplimentary characters, so this is easy.

                // We could however have 1 extra byte if the last call had an encoder and a funky fallback and
                // if we don't use the funky fallback this time.

                // Do we have an extra char left over from last time?
                if (charLeftOver > 0)
                    charCount++;

                return (charCount);
            }

            // Count is more complicated if you have a funky fallback
            // For fallback we may need a fallback buffer, we know we're not default fallback
            int byteCount = 0;

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                Debug.Assert(Char.IsHighSurrogate(charLeftOver), "[ASCIIEncoding.GetByteCount]leftover character should be high surrogate");
                Debug.Assert(encoder != null, "[ASCIIEncoding.GetByteCount]Expected encoder");

                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, false);

                // This will fallback a pair if *chars is a low surrogate
                charsForFallback = chars; // Avoid passing chars by reference to allow it to be enregistered
                fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                chars = charsForFallback;
            }

            // Now we may have fallback char[] already from the encoder

            // Go ahead and do it, including the fallback.
            char ch;
            while ((ch = (null == fallbackBuffer) ? default : fallbackBuffer.InternalGetNextChar()) != 0 ||
                    chars < charEnd &&
                    false == TryEncodeAsciiCharsToBytes(chars, null, byteCount, false))
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Check for fallback, this'll catch surrogate pairs too.
                // no chars >= 0x80 are allowed.
                if (ch > 0x7f)
                {
                    if (null == fallbackBuffer)
                    {
                        // Initialize the buffer
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, false);
                    }

                    // Get Fallback
                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be enregistered
                    fallbackBuffer.InternalFallback(ch, ref charsForFallback);
                    chars = charsForFallback;
                    continue;
                }

                // We'll use this one
                byteCount++;
            }

            Debug.Assert(null == fallbackBuffer || fallbackBuffer.Remaining == 0,
                "[ASCIIEncoding.GetByteCount]Expected Empty fallback buffer");

            return byteCount;
        }

        // Optimized by TryEncodeAsciiCharsToBytes
        internal override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(bytes != null, "[ASCIIEncoding.GetBytes]bytes is null");
            Debug.Assert(byteCount >= 0, "[ASCIIEncoding.GetBytes]byteCount is negative");
            Debug.Assert(chars != null, "[ASCIIEncoding.GetBytes]chars is null");
            Debug.Assert(charCount >= 0, "[ASCIIEncoding.GetBytes]charCount is negative");

            // Assert because we shouldn't be able to have a null encoder.
            Debug.Assert(encoderFallback != null, "[ASCIIEncoding.GetBytes]Attempting to use null encoder fallback");

            // Get any left over characters
            char charLeftOver = default;
            EncoderReplacementFallback fallback = null;

            // For fallback we may need a fallback buffer, we know we aren't default fallback.
            EncoderFallbackBuffer fallbackBuffer = null;
            char* charsForFallback;

            // prepare our end
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            if (encoder != null)
            {
                charLeftOver = encoder._charLeftOver;
                fallback = encoder.Fallback as EncoderReplacementFallback;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder._throwOnOverflow)
                        throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                }

                Debug.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[ASCIIEncoding.GetBytes]leftover character should be high surrogate");

                // Verify that we have no fallbackbuffer, for ASCII its always empty, so just assert
                Debug.Assert(!encoder._throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetBytes]Expected empty fallback buffer");
            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }


            // See if we do the fast default or slightly slower fallback
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Fast version
                char cReplacement = fallback.DefaultString[0];

                // Check for replacements in range, otherwise fall back to slow version.
                if (cReplacement <= (char)0x7f)
                {
                    // We should have exactly as many output bytes as input bytes, unless there's a left
                    // over character, in which case we may need one more.
                    // If we had a left over character will have to add a ?  (This happens if they had a funky
                    // fallback last time, but not this time.) (We can't spit any out though
                    // because with fallback encoder each surrogate is treated as a seperate code point)
                    if (charLeftOver > 0)
                    {
                        // Have to have room
                        // Throw even if doing no throw version because this is just 1 char,
                        // so buffer will never be big enough
                        if (0 == byteCount)
                            ThrowBytesOverflow(encoder, true);

                        // This'll make sure we still have more room and also make sure our return value is correct.
                        *(bytes++) = (byte)cReplacement;
                        byteCount--;                // We used one of the ones we were counting.
                    }

                    // This keeps us from overrunning our output buffer
                    if (byteCount < charCount)
                    {
                        // Throw or make buffer smaller?
                        ThrowBytesOverflow(encoder, byteCount < 1);

                        // Just use what we can
                        charEnd = chars + byteCount;
                    }

                    // We just do a quick copy

                    while (chars < charEnd && false == TryEncodeAsciiCharsToBytes(chars, bytes, byteCount))
                    {
                        char ch2 = *(chars++);
                        if (ch2 >= 0x0080) *(bytes++) = (byte)cReplacement;
                        else *(bytes++) = unchecked((byte)(ch2));
                        // Adjust the amount of bytes remaining
                        byteCount = (int)(bytes - byteStart);
                    }

                    // Clear encoder
                    if (encoder != null)
                    {
                        encoder._charLeftOver = default;
                        encoder._charsUsed = (int)(chars - charStart);
                    }

                    return (int)(bytes - byteStart);
                }
            }

            // Slower version, have to do real fallback.

            // prepare our end
            byte* byteEnd = bytes + byteCount;

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                // Initialize the buffer
                Debug.Assert(encoder != null,
                    "[ASCIIEncoding.GetBytes]Expected non null encoder if we have surrogate left over");
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, true);

                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                // This will fallback a pair if *chars is a low surrogate
                charsForFallback = chars; // Avoid passing chars by reference to allow it to be enregistered
                fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                chars = charsForFallback;
            }

            // Now we may have fallback char[] already from the encoder

            // Go ahead and do it, including the fallback.
            char ch;
            while ((ch = (null == fallbackBuffer) ? default : fallbackBuffer.InternalGetNextChar()) != 0 ||
                    chars < charEnd && 
                    false == TryEncodeAsciiCharsToBytes(chars, bytes, byteCount))
            {                
                // First unwind any fallback
                if (default == ch)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Check for fallback, this'll catch surrogate pairs too.
                // All characters >= 0x80 must fall back.
                if (ch > 0x7f)
                {
                    // Initialize the buffer
                    if (null == fallbackBuffer)
                    {
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, true);
                    }

                    // Get Fallback
                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be enregistered
                    fallbackBuffer.InternalFallback(ch, ref charsForFallback);
                    chars = charsForFallback;

                    // Go ahead & continue (& do the fallback)
                    continue;
                }

                // We'll use this one
                // Bounds check
                if (bytes >= byteEnd)
                {
                    // didn't use this char, we'll throw or use buffer
                    if (null == fallbackBuffer || fallbackBuffer.bFallingBack == false)
                    {
                        Debug.Assert(chars > charStart || bytes == byteStart,
                            "[ASCIIEncoding.GetBytes]Expected chars to have advanced already.");
                        chars--;                                        // don't use last char
                    }
                    else
                        fallbackBuffer.MovePrevious();

                    // Are we throwing or using buffer?
                    ThrowBytesOverflow(encoder, bytes == byteStart);    // throw?
                    break;                                              // don't throw, stop
                }

                // Go ahead and add it
                *bytes = unchecked((byte)ch);
                bytes++;

                // Adjust the amount of bytes remaining
                byteCount = (int)(bytes - byteStart);
            }

            // Need to do encoder stuff
            if (encoder != null)
            {
                // Fallback stuck it in encoder if necessary, but we have to clear MustFlush cases
                if (fallbackBuffer != null && false == fallbackBuffer.bUsedEncoder)
                    // Clear it in case of MustFlush
                    encoder._charLeftOver = default;

                // Set our chars used count
                encoder._charsUsed = (int)(chars - charStart);
            }

            Debug.Assert(null == fallbackBuffer || fallbackBuffer.Remaining == 0 ||
                (encoder != null && false == encoder._throwOnOverflow),
                "[ASCIIEncoding.GetBytes]Expected Empty fallback buffer at end");

            return (int)(bytes - byteStart);
        }

        // This is internal and called by something else,
        // Optimized by TryGetAsciiChars
        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS decoder)
        {
            // Just assert, we're called internally so these should be safe, checked already
            Debug.Assert(bytes != null, "[ASCIIEncoding.GetCharCount]bytes is null");
            Debug.Assert(count >= 0, "[ASCIIEncoding.GetCharCount]byteCount is negative");

            // ASCII doesn't do best fit, so don't have to check for it, find out which decoder fallback we're using
            DecoderReplacementFallback fallback = null;

            if (null == decoder)
                fallback = this.DecoderFallback as DecoderReplacementFallback;
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                Debug.Assert(false == decoder._throwOnOverflow | false == decoder.InternalHasFallbackBuffer |
                    decoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetCharCount]Expected empty fallback buffer");
            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Just return length, SBCS stay the same length because they don't map to surrogate
                // pairs and we don't have a decoder fallback.

                return count;
            }

            // Only need decoder fallback buffer if not using default replacement fallback, no best fit for ASCII
            DecoderFallbackBuffer fallbackBuffer = null;

            // Have to do it the hard way.
            // Assume charCount will be == count
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(1);

            // Do it our fast way
            byte* byteEnd = bytes + count;

            // Quick loop
            while (bytes < byteEnd && false == TryGetAsciiChars(bytes, null, count, false))
            {
                // Faster if don't use *bytes++;
                byte b = *bytes;
                bytes++;

                // If unknown we have to do fallback count
                if (b >= 0x80)
                {
                    if (null == fallbackBuffer)
                    {
                        if (null == decoder)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - count, null);
                    }

                    // Use fallback buffer
                    byteBuffer[0] = b;
                    count--;            // Have to unreserve the one we already allocated for b
                    count += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                }
            }

            // Fallback buffer must be empty
            Debug.Assert(null == fallbackBuffer || fallbackBuffer.Remaining == 0,
                "[ASCIIEncoding.GetCharCount]Expected Empty fallback buffer");

            // Converted sequence is same length as input
            return count;
        }

        // Hackathon, Issue# 18208 - juliusfriedman@gmail.com
        // Sourced from the implementations provided by benadams.
        // Alternate implementations in C++ @ https://git.merproject.org/mer-core/qtbase/commit/34821e226a94858480e57bb25ac7655bfd19f1e6 with support for UTF-8 Etc for reference.

        //https://github.com/benaadams/FrameworkBenchmarks/blob/9d940f533c14130ab5b627b61ab142c90abfbac3/frameworks/CSharp/aspnetcore-mono/PlatformBenchmarks/Utilities/BufferExtensionsText.cs#L245-L330

        // Encode as bytes upto the first non-ASCII byte with respect to length, only writes to output if `write` is true.
        private static unsafe bool TryEncodeAsciiCharsToBytes(char* input, byte* output, int length, bool write = true)
        {
            // Note: Not BIGENDIAN
            const int Shift16Shift24 = (1 << 16) | (1 << 24);
            const int Shift8Identity = (1 << 8) | (1);

            var i = 0;

            if (length < 4)
                goto trailing;

            var unaligned = (int)(((ulong)input) & 0x7) >> 1;
            // Unaligned chars
            for (; i < unaligned; i++)
            {
                var ch = input[i];
                if (ch > 0x7f)
                {
                    goto exit; // Non-ascii
                }
                if(write) output[i] = (byte)ch; // Cast convert
            }

            // Aligned
            int ulongDoubleCount = (length - i) & ~0x7;
            for (; i < ulongDoubleCount; i += 8)
            {
                ulong inputUlong0 = *(ulong*)(input + i);
                ulong inputUlong1 = *(ulong*)(input + i + 4);
                // Pack 16 ASCII chars into 16 bytes
                if ((inputUlong0 & 0xFF80FF80FF80FF80u) == 0)
                {
                    if (write)
                        *(uint*)(output + i) =
                        ((uint)((inputUlong0 * Shift16Shift24) >> 24) & 0xffff) |
                        ((uint)((inputUlong0 * Shift8Identity) >> 24) & 0xffff0000);
                }
                else
                {
                    goto exit; // Non-ascii
                }
                if ((inputUlong1 & 0xFF80FF80FF80FF80u) == 0)
                {
                    if (write)
                        *(uint*)(output + i + 4) =
                        ((uint)((inputUlong1 * Shift16Shift24) >> 24) & 0xffff) |
                        ((uint)((inputUlong1 * Shift8Identity) >> 24) & 0xffff0000);
                }
                else
                {
                    i += 4;
                    goto exit; // Non-ascii
                }
            }
            if (length - 4 > i)
            {
                ulong inputUlong = *(ulong*)(input + i);
                if ((inputUlong & 0xFF80FF80FF80FF80u) == 0)
                {
                    // Pack 8 ASCII chars into 8 bytes
                    if (write)
                        *(uint*)(output + i) =
                        ((uint)((inputUlong * Shift16Shift24) >> 24) & 0xffff) |
                        ((uint)((inputUlong * Shift8Identity) >> 24) & 0xffff0000);
                }
                else
                {
                    goto exit; // Non-ascii
                }

                i += 4;
            }

        trailing:
            for (; i < length; i++)
            {
                var ch = input[i];
                if (ch > 0x7f)
                {
                    goto exit; // Hit non-ascii
                }

                if (write)
                    output[i] = (byte)ch; // Cast convert
            }

        exit:
            //consumed = i;
            return length == i ? true : false;
        }

        //https://github.com/aspnet/KestrelHttpServer/blob/0aff4a0440c2f393c0b98e9046a8e66e30a56cb0/src/Kestrel.Core/Internal/Infrastructure/StringUtilities.cs#L12-L110

        // Encode as char upto the first non-ASCII byte with respect to count, only writes to output if `write` is true.
        static unsafe bool TryGetAsciiChars(byte* input, char* output, int count, bool write = true)
        {
            // Calculate end position
            var end = input + count;
            // Start as valid
            var isValid = true;

            do
            {
                // If Vector not-accelerated or remaining less than vector size
                if (false == Vector.IsHardwareAccelerated || input > end - Vector<sbyte>.Count)
                {
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                    {
                        // 64-bit: Loop longs by default
                        while (input <= end - sizeof(long))
                        {
                            isValid &= CheckBytesInAsciiRange(((long*)input)[0]);

                            if (false == isValid)
                                return isValid;

                            if (write)
                            {
                                output[0] = (char)input[0];
                                output[1] = (char)input[1];
                                output[2] = (char)input[2];
                                output[3] = (char)input[3];
                                output[4] = (char)input[4];
                                output[5] = (char)input[5];
                                output[6] = (char)input[6];
                                output[7] = (char)input[7];
                            }

                            input += sizeof(long);
                            output += sizeof(long);
                        }
                        if (input <= end - sizeof(int))
                        {
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

                            if (false == isValid)
                                return isValid;

                            if (write)
                            {
                                output[0] = (char)input[0];
                                output[1] = (char)input[1];
                                output[2] = (char)input[2];
                                output[3] = (char)input[3];
                            }

                            input += sizeof(int);
                            output += sizeof(int);
                        }
                    }
                    else
                    {
                        // 32-bit: Loop ints by default
                        while (input <= end - sizeof(int))
                        {
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

                            if (false == isValid)
                                return isValid;

                            if (write)
                            {
                                output[0] = (char)input[0];
                                output[1] = (char)input[1];
                                output[2] = (char)input[2];
                                output[3] = (char)input[3];
                            }

                            input += sizeof(int);
                            output += sizeof(int);
                        }
                    }
                    if (input <= end - sizeof(short))
                    {
                        isValid &= CheckBytesInAsciiRange(((short*)input)[0]);

                        if (false == isValid)
                            return isValid;

                        if (write)
                        {
                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                        }

                        input += sizeof(short);
                        output += sizeof(short);
                    }
                    if (input < end)
                    {
                        isValid &= CheckBytesInAsciiRange(((sbyte*)input)[0]);
                        if (false == isValid)
                            return isValid;
                        if (write)
                            output[0] = (char)input[0];
                    }

                    return isValid;
                }

                // do/while as entry condition already checked
                do
                {
                    var vector = Unsafe.AsRef<Vector<sbyte>>(input);
                    isValid &= CheckBytesInAsciiRange(vector);
                    if (false == isValid)
                        return isValid;
                    if (write)
                    Vector.Widen(
                        vector,
                        out Unsafe.AsRef<Vector<short>>(output),
                        out Unsafe.AsRef<Vector<short>>(output + Vector<short>.Count));
                    input += Vector<sbyte>.Count;
                    output += Vector<sbyte>.Count;
                } while (input <= end - Vector<sbyte>.Count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while (input < end);

            return isValid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
        {
            // Vectorized byte range check, signed byte > 0 for 1-127
            return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
        }

        // Validate: bytes != 0 && bytes <= 127
        //  Subtract 1 from all bytes to move 0 to high bits
        //  bitwise or with self to catch all > 127 bytes
        //  mask off high bits and check if 0

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(long check)
        {
            const long HighBits = unchecked((long)0x8080808080808080L);
            return (((check - 0x0101010101010101L) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(int check)
        {
            const int HighBits = unchecked((int)0x80808080);
            return (((check - 0x01010101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(short check)
        {
            const short HighBits = unchecked((short)0x8080);
            return (((short)(check - 0x0101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(sbyte check) => check > 0;

        // Optimized by TryGetAsciiChars
        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS decoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(bytes != null, "[ASCIIEncoding.GetChars]bytes is null");
            Debug.Assert(byteCount >= 0, "[ASCIIEncoding.GetChars]byteCount is negative");
            Debug.Assert(chars != null, "[ASCIIEncoding.GetChars]chars is null");
            Debug.Assert(charCount >= 0, "[ASCIIEncoding.GetChars]charCount is negative");

            // Do it fast way if using ? replacement fallback
            byte* byteEnd = bytes + byteCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // Note: ASCII doesn't do best fit, but we have to fallback if they use something > 0x7f
            // Only need decoder fallback buffer if not using ? fallback.
            // ASCII doesn't do best fit, so don't have to check for it, find out which decoder fallback we're using
            DecoderReplacementFallback fallback = null;
            char* charsForFallback;

            if (null == decoder)
                fallback = this.DecoderFallback as DecoderReplacementFallback;
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                Debug.Assert(false == decoder._throwOnOverflow || 
                    false == decoder.InternalHasFallbackBuffer ||
                    decoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetChars]Expected empty fallback buffer");
            }

            //Empty string is not allowed as replacement.

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Try it the fast way
                char replacementChar = fallback.DefaultString[0];

                // Need byteCount chars, otherwise too small buffer
                if (charCount < byteCount)
                {
                    // Need at least 1 output byte, throw if must throw
                    ThrowCharsOverflow(decoder, charCount < 1);

                    // Not throwing, use what we can
                    byteEnd = bytes + charCount;
                }

                // Quick loop, just do 'replacementChar' replacement because we don't have fallbacks for decodings.
                // Perform the loop while there is an invalid ASCII char * bytes.
                while (bytes < byteEnd && false == TryGetAsciiChars(bytes, chars, charCount))
                {
                    byte b = *(bytes++);
                    if (b >= 0x80)
                        // This is an invalid byte in the ASCII encoding.
                        *(chars++) = replacementChar;
                    else
                        *(chars++) = unchecked((char)b);
                    //Adjust the amount of chars remaining.
                    charCount = (int)(chars - charStart);
                }

                // bytes & chars used are the same
                if (decoder != null)
                    decoder._bytesUsed = (int)(bytes - byteStart);
                return (int)(chars - charStart);
            }

            // Slower way's going to need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(1);
            char* charEnd = chars + charCount;

            // Not quite so fast loop when TryGetAsciiChars is false.
            while (bytes < byteEnd && false == TryGetAsciiChars(bytes, chars, charCount))
            {
                // Faster if don't use *bytes++;
                byte b = *(bytes);
                bytes++;

                if (b >= 0x80)
                {
                    // This is an invalid byte in the ASCII encoding.
                    if (null == fallbackBuffer)
                    {
                        if (null == decoder)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - byteCount, charEnd);
                    }

                    // Use fallback buffer
                    byteBuffer[0] = b;

                    // Note that chars won't get updated unless this succeeds
                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be enregistered
                    if(false == fallbackBuffer.InternalFallback(byteBuffer, bytes, ref charsForFallback))
                    {
                        // May or may not throw, but we didn't get this byte
                        Debug.Assert(bytes > byteStart || chars == charStart,
                            "[ASCIIEncoding.GetChars]Expected bytes to have advanced already (fallback case)");
                        bytes--;                                            // unused byte
                        fallbackBuffer.InternalReset();                     // Didn't fall this back
                        ThrowCharsOverflow(decoder, chars == charStart);    // throw?
                        break;
                    }
                    chars = charsForFallback;
                }
                else
                {
                    // Make sure we have buffer space
                    if (chars >= charEnd)
                    {
                        Debug.Assert(bytes > byteStart || chars == charStart,
                            "[ASCIIEncoding.GetChars]Expected bytes to have advanced already (normal case)");
                        bytes--;                                            // unused byte
                        ThrowCharsOverflow(decoder, chars == charStart);    // throw?
                        break;                                              // don't throw, but stop loop
                    }

                    *(chars) = unchecked((char)b);
                    chars++;
                }
                //Adjust the amount of chars remaining
                charCount = (int)(chars - charStart);
            }

            // Might have had decoder fallback stuff.
            if (null != decoder)
                decoder._bytesUsed = (int)(bytes - byteStart);

            // Expect Empty fallback buffer for GetChars
            Debug.Assert(null == fallbackBuffer || fallbackBuffer.Remaining == 0,
                "[ASCIIEncoding.GetChars]Expected Empty fallback buffer");

            return (int)(chars - charStart);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less.
            return (byteCount *= EncoderFallback.MaxCharCount) > int.MaxValue 
                ? 
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow) 
                : 
                (int)byteCount;
        }


        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Just return length, SBCS stay the same length because they don't map to surrogate
            long charCount = byteCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less, unknown fallbacks could be longer.
            return (charCount *= DecoderFallback.MaxCharCount) > int.MaxValue 
                ? 
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow) 
                : 
                (int)charCount;
        }

        // True if and only if the encoding only uses single byte code points.  (Ie, ASCII, 1252, etc)

        public override bool IsSingleByte
        {
            get => true;
        }

        public override Decoder GetDecoder() => new DecoderNLS(this);


        public override Encoder GetEncoder() => new EncoderNLS(this);
    }
}
