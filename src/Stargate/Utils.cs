using AssetsTools.NET;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using System.Runtime.InteropServices;

public class Utils
{
    public static byte[] FlipImage(byte[] bgraData, int height, int width)
    {
        byte[] data = new byte[bgraData.Length];

        for (int k = 0; k < height; k++)
        {
            int j = height - k - 1;
            Buffer.BlockCopy(bgraData, k * width * 4, data, j * width * 4, width * 4);
        }

        return data;
    }

    public static void PrepareTexture(string texturePath, ref AssetTypeValueField baseField)
    {
        var image = Image.Load<Bgra32>(texturePath);
        PrepareTexture(image, ref baseField, texturePath.Split(".")[0]);

    }

    public static void PrepareTexture(Image<Bgra32> image, ref AssetTypeValueField baseField, string Name )
    {
        var memGroup = image.GetPixelMemoryGroup().ToArray()[0];
        byte[] bgraData = MemoryMarshal.AsBytes(memGroup.Span).ToArray();

        bgraData = FlipImage(bgraData, image.Width, image.Height);
        // TODO: Check why its necessary to flip the bgra data?

        var tex = new TextureFile();
        tex.m_TextureFormat = (int)TextureFormat.RGBA32;
        tex.SetTextureData(bgraData, image.Width, image.Height);

        tex.m_Name = Name;
        tex.m_ForcedFallbackFormat = 4;
        tex.m_DownscaleFallback = false;
        tex.m_MipCount = 1;
        tex.m_IsReadable = false;
        tex.m_StreamingMipmaps = false;
        tex.m_StreamingMipmapsPriority = 0;
        tex.m_ImageCount = 1;
        tex.m_TextureDimension = 2;
        tex.m_TextureSettings.m_FilterMode = 1;
        tex.m_TextureSettings.m_Aniso = 1;
        tex.m_TextureSettings.m_MipBias = 0;
        tex.m_TextureSettings.m_WrapU = 1;
        tex.m_TextureSettings.m_WrapV = 1;
        tex.m_TextureSettings.m_WrapW = 1;
        tex.m_LightmapFormat = 0;
        tex.m_ColorSpace = 1;
        tex.m_StreamData.path = "";

        tex.WriteTo(baseField);
    }
}
