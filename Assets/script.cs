using UnityEngine;
using UnityEngine.UI;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.VertexSource;

public class script : MonoBehaviour
{
    // Unity texture
    private Texture2D texture;

    // agg-sharp buffer
    private ImageBuffer buffer;

    // texture size
    private const int WIDTH = 512;
    private const int HEIGHT = 512;

    // animation counter
    private float x = 0;

    void Start()
    {
        // create new texture for the Unity3D Image
        Image image = GetComponent<Image>();
        texture = new Texture2D(WIDTH, HEIGHT, TextureFormat.ARGB32, false, false);
        image.material.mainTexture = texture;

        // create agg-sharp buffer
        buffer = new ImageBuffer(WIDTH, HEIGHT, 32, new BlenderBGRA());
    }

    void Update()
    {
        // update animation counter
        x += Time.deltaTime;
        while (x > 1.0f) x -= 1.0f;

        // clear background with white
        Graphics2D g = buffer.NewGraphics2D();
        g.Clear(RGBA_Bytes.White);

        // draw some lines
        float w = buffer.Width * x;
        for (int i = 0; i < 10; i++) {
            g.Line(x1: 0, y1: buffer.Height * i / 10,
            x2: w - w * i / 10, y2: 0,
            color: RGBA_Bytes.Black, strokeWidth: 3);
        }

        // draw some text
        TypeFacePrinter textPrinter = new TypeFacePrinter("Hello World!", 30, justification: Justification.Center);
        IVertexSource translatedText = new VertexSourceApplyTransform(textPrinter, Affine.NewTranslation(buffer.Width / 2, 5));
        g.Render(translatedText, RGBA_Bytes.Blue);

        // update texture data
        byte[] pixels = buffer.GetBuffer();
        texture.LoadRawTextureData(pixels);
        texture.Apply();
    }
}
