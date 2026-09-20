using UnityEngine;

namespace qoooo.Rendering.Sources
{
    public partial class PosterCanvas
    {
        private void BuildEditorialLayout(Texture texture)
        {
            var width = Width;
            var height = Height;
            var unit = SizeUnit;
            var margin = width * 0.04f;
            var column = (width - margin * 2f) / 12f;

            SolidRect(0f, 0f, width, height, new Color(0.035f, 0.045f, 0.07f, 1f));
            SolidRect(margin + column * 2f, height * 0.28f, column * 8f, height * 0.54f,
                new Color(0.08f, 0.42f, 0.38f, 1f));
            SolidRect(0f, height * 0.87f, width, height * 0.13f,
                new Color(0.9f, 0.22f, 0.16f, 1f));
            SolidRect(margin, height * 0.16f, column * 1.8f, unit * 0.018f,
                new Color(1f, 0.78f, 0.2f, 1f), 4f);

            Image(texture, margin + column * 3f, height * 0.3f, column * 5.6f, height * 0.53f,
                PosterFit.Cover);
            Image(texture, margin, height * 0.43f, column * 2.15f, column * 2.15f,
                PosterFit.Cover);
            Image(texture, margin + column * 9.65f, height * 0.36f, column * 1.7f, height * 0.3f,
                texture.width * 0.5f, texture.height * 0.1f,
                texture.width * 0.5f, texture.height * 0.8f, 9f);
            Image(texture, margin + column * 0.8f, height * 0.7f, column * 2.7f, height * 0.11f,
                PosterFit.Cover);

            Line(margin, height * 0.24f, width - margin, height * 0.24f,
                unit * 0.004f, Color.white);
            Line(width - margin - column * 0.7f, height * 0.3f,
                width - margin, height * 0.78f, unit * 0.006f,
                new Color(1f, 0.78f, 0.2f, 1f));
            Text("QOOOO", width - margin, height * 0.045f, unit * 0.12f, Color.white,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Top);
            Text("LIVE VISUAL / 026", width * 0.5f, height * 0.185f, unit * 0.025f,
                new Color(1f, 0.78f, 0.2f, 1f),
                PosterHorizontalAlign.Center, PosterVerticalAlign.Top, -6f);
            Text("GENERATIVE VISUAL SYSTEM", margin, height * 0.91f, unit * 0.025f,
                new Color(0.035f, 0.045f, 0.07f, 1f),
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top);
        }

        private void BuildSplitLayout(Texture texture)
        {
            var width = Width;
            var height = Height;
            var unit = SizeUnit;
            var margin = width * 0.05f;
            var gap = width * 0.025f;
            var leftWidth = width * 0.58f - margin;
            var rightX = margin + leftWidth + gap;
            var rightWidth = width - margin - rightX;

            SolidRect(0f, 0f, width, height, new Color(0.05f, 0.035f, 0.07f, 1f));
            SolidRect(margin, height * 0.2f, leftWidth, height * 0.61f,
                new Color(0.12f, 0.16f, 0.42f, 1f));
            SolidRect(rightX, height * 0.2f, rightWidth, height * 0.61f,
                new Color(0.86f, 0.2f, 0.19f, 1f));
            SolidRect(0f, height * 0.86f, width, height * 0.14f,
                new Color(0.95f, 0.76f, 0.18f, 1f));

            Image(texture, margin, height * 0.2f, leftWidth, height * 0.61f,
                PosterFit.Cover);
            Image(texture, rightX, height * 0.29f, rightWidth, height * 0.27f,
                PosterFit.Cover, -7f);
            Image(texture, rightX, height * 0.6f, rightWidth, height * 0.16f,
                texture.width * 0.2f, texture.height * 0.25f,
                texture.width * 0.6f, texture.height * 0.5f);

            Line(margin, height * 0.16f, width - margin, height * 0.16f,
                unit * 0.005f, Color.white);
            Text("SPLIT / 026", margin, height * 0.055f, unit * 0.09f, Color.white,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top);
            Text("TWO CHANNEL STUDY", margin, height * 0.9f, unit * 0.028f,
                new Color(0.05f, 0.035f, 0.07f, 1f),
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top);
        }

        private void BuildModularLayout(Texture texture)
        {
            var width = Width;
            var height = Height;
            var unit = SizeUnit;
            var margin = width * 0.04f;
            var gap = width * 0.018f;
            var block = (width - margin * 2f - gap * 3f) / 4f;

            SolidRect(0f, 0f, width, height, new Color(0.025f, 0.055f, 0.055f, 1f));
            SolidRect(margin, height * 0.27f, block * 1.4f, height * 0.22f,
                new Color(0.14f, 0.53f, 0.48f, 1f));
            SolidRect(margin + (block + gap) * 2f, height * 0.55f, block * 1.85f, height * 0.2f,
                new Color(0.93f, 0.28f, 0.17f, 1f), 3f);

            Image(texture, margin + block + gap, height * 0.06f, block * 1.8f, height * 0.31f,
                PosterFit.Cover);
            Image(texture, margin, height * 0.53f, block * 1.35f, height * 0.28f,
                texture.width * 0.1f, texture.height * 0.05f,
                texture.width * 0.55f, texture.height * 0.7f, -8f);
            Image(texture, margin + (block + gap) * 2.6f, height * 0.2f, block * 1.4f, height * 0.25f,
                PosterFit.Cover, 6f);
            Image(texture, margin + (block + gap) * 1.35f, height * 0.62f, block * 1.05f, height * 0.19f,
                PosterFit.Cover);

            Line(margin, height * 0.88f, width - margin, height * 0.88f,
                unit * 0.005f, new Color(0.95f, 0.76f, 0.18f, 1f));
            Text("MODULAR", margin, height * 0.07f, unit * 0.065f, Color.white,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top);
            Text("04 / SHARED SIGNALS", width - margin, height * 0.9f, unit * 0.028f,
                new Color(0.95f, 0.76f, 0.18f, 1f),
                PosterHorizontalAlign.Right, PosterVerticalAlign.Top);
            Text("QOOOO", margin + block * 0.1f, height * 0.31f, unit * 0.04f,
                new Color(0.025f, 0.055f, 0.055f, 1f),
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, 4f);
        }
    }
}
