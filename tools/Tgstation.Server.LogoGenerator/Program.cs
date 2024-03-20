// Simplest app 2024
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

using var bitmap = svg.Draw(64, 64);
using var whiteBgBitmap = whiteBgSvg.Draw(160, 160);

using var icon = Icon.FromHandle(whiteBgBitmap.GetHicon());

Directory.CreateDirectory("artifacts");
await using var iconStream = new FileStream("artifacts/tgs.ico", FileMode.Create);
bitmap.Save("artifacts/tgs.png", ImageFormat.Png);

icon.Save(iconStream);
