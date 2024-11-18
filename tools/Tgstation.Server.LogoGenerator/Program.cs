// Simplest app 2024
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Svg;

var svgContents = await File.ReadAllTextAsync("build/logo.svg");
var whiteBgSvgContents = svgContents
	.Replace("<!-- DO NOT CHANGE THIS LINE, UNCOMMENTING IT ENABLES THE WHITE BACKGROUND FOR THE .ICO--><!--", String.Empty)
	.Replace("SCRIPT_REPLACE_TOKEN", String.Empty);

await using var pngMs =
	new MemoryStream(
		Encoding.UTF8.GetBytes(svgContents));
await using var icoMs =
	new MemoryStream(
		Encoding.UTF8.GetBytes(whiteBgSvgContents));

var svg = SvgDocument.Open<SvgDocument>(pngMs);
var whiteBgSvg = SvgDocument.Open<SvgDocument>(icoMs);

using var bitmap = svg.Draw(128, 128);
using var whiteBgBitmap = whiteBgSvg.Draw(160, 160);

// https://stackoverflow.com/questions/21387391/how-to-convert-an-image-to-an-icon-without-losing-transparency/21389253#21389253
// slight modifications
static Icon IconFromImage(Image img)
{
	using var ms = new MemoryStream();
	using var bw = new BinaryWriter(ms);
	// Header
	bw.Write((short)0);   // 0 : reserved
	bw.Write((short)1);   // 2 : 1=ico, 2=cur
	bw.Write((short)1);   // 4 : number of images
						  // Image directory
	var w = img.Width;
	if (w >= 256) throw new InvalidOperationException("Width too big!");
	bw.Write((byte)w);    // 0 : width of image
	var h = img.Height;
	if (h >= 256) throw new InvalidOperationException("Height too big!");
	bw.Write((byte)h);    // 1 : height of image
	bw.Write((byte)0);    // 2 : number of colors in palette
	bw.Write((byte)0);    // 3 : reserved
	bw.Write((short)0);   // 4 : number of color planes
	bw.Write((short)0);   // 6 : bits per pixel
	var sizeHere = ms.Position;
	bw.Write(0);     // 8 : image size
	var start = (int)ms.Position + 4;
	bw.Write(start);      // 12: offset of image data
						  // Image data
	img.Save(ms, ImageFormat.Png);
	var imageSize = (int)ms.Position - start;
	ms.Seek(sizeHere, SeekOrigin.Begin);
	bw.Write(imageSize);
	ms.Seek(0, SeekOrigin.Begin);

	// And load it
	return new Icon(ms);
}

using var icon = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
	? IconFromImage(whiteBgBitmap)
	: Icon.FromHandle(whiteBgBitmap.GetHicon());

Directory.CreateDirectory("artifacts");
await using var iconStream = new FileStream("artifacts/tgs.ico", FileMode.Create);
bitmap.Save("artifacts/tgs.png", ImageFormat.Png);

icon.Save(iconStream);
