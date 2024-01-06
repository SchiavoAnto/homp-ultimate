using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CustomMediaPlayerUltimate;

internal class Utils
{
    //Thank you ChatGPT
    public static Dictionary<string, string> ReadID3v2Tags(string filePath)
    {
        // Create a dictionary to store the tags
        Dictionary<string, string> tags = new Dictionary<string, string>();

        // Open the MP3 file
        using (FileStream stream = new FileStream(filePath, FileMode.Open))
        {
            // Read the ID3v2 tag header
            byte[] header = new byte[10];
            stream.Read(header, 0, 10);

            // Check if the file has an ID3v2 tag
            if (Encoding.ASCII.GetString(header, 0, 3) == "ID3")
            {
                // Get the ID3v2 tag size
                int size = GetTagSize(header, 6);

                // Read the ID3v2 tag
                byte[] tagData = new byte[size];
                stream.Read(tagData, 0, size);

                // Read the ID3v2 frames
                int offset = 0;
                while (offset < size)
                {
                    // Read the frame header
                    byte[] frameHeader = new byte[10];
                    for (int i = 0; i < 10; i++)
                    {
                        if (offset + i >= size)
                        {
                            break;
                        }
                        frameHeader[i] = tagData[offset + i];
                    }

                    // Get the frame size and ID
                    int frameSize = GetTagSize(frameHeader, 4);
                    string frameID = Encoding.ASCII.GetString(frameHeader, 0, 4);

                    // Read the frame data
                    byte[] frameData = new byte[frameSize];
                    for (int i = 0; i < frameSize; i++)
                    {
                        if (offset + 10 + i >= size)
                        {
                            break;
                        }
                        frameData[i] = tagData[offset + 10 + i];
                    }

                    // Decode the frame and add it to the dictionary
                    string frameValue = DecodeFrame(frameID, frameData);
                    tags[frameID] = frameValue;

                    // Move to the next frame
                    offset += 10 + frameSize;
                }
            }
        }

        return tags;
    }

    //Thank you ChatGPT
    public static int GetTagSize(byte[] header, int offset)
    {
        // The ID3v2 tag size is stored in the header as a 4-byte synchsafe integer
        // (a synchsafe integer is a regular integer where each byte has its most significant bit set to 0,
        // so it can't contain any synchronization bytes)
        int size = 0;
        size |= (header[offset] & 0x7F) << 21;
        size |= (header[offset + 1] & 0x7F) << 14;
        size |= (header[offset + 2] & 0x7F) << 7;
        size |= header[offset + 3] & 0x7F;
        return size;
    }

    //Thank you ChatGPT
    public static string DecodeFrame(string frameID, byte[] frameData)
    {
        // The frame data is encoded in either ISO-8859-1 or UTF-8
        Encoding encoding;
        if (frameID[0] == 'T')
        {
            // ISO-8859-1 for text frames (e.g. TIT2, TALB)
            encoding = Encoding.GetEncoding("ISO-8859-1");
        }
        else
        {
            // UTF-8 for all other frames
            encoding = Encoding.UTF8;
        }

        // Decode the frame data and remove the null terminator
        string value = encoding.GetString(frameData).TrimEnd('\0');
        return value;
    }
}
