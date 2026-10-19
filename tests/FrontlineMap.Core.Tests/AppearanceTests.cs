using System;
using FrontlineMap.Core;
using Xunit;
public class AppearanceTests
{
 [Fact] public void CustomColorsPreserveCoverageAndClassification(){var a=new OverlayAppearance("#0011AA","cc2200","008899","ffeedd");foreach(var kind in new[]{PixelKind.Friendly,PixelKind.Enemy,PixelKind.Balanced,PixelKind.Front}){var input=new Pixel(1,2,3,73,kind);var output=a.Apply(input);Assert.Equal((byte)73,output.A);Assert.Equal(kind,output.Kind);}var friendly=a.Apply(new Pixel(1,2,3,73,PixelKind.Friendly));Assert.Equal((byte)0,friendly.R);Assert.Equal((byte)17,friendly.G);Assert.Equal((byte)170,friendly.B);Assert.Equal("CC2200",a.Enemy);}
 [Fact] public void UnsupportedSpaceStaysTransparent(){var p=OverlayAppearance.Default.Apply(default(Pixel));Assert.Equal((byte)0,p.A);Assert.Equal(PixelKind.Unsupported,p.Kind);}
 [Theory][InlineData(null)][InlineData("")][InlineData("FFF")][InlineData("##123456")][InlineData("GG0000")][InlineData("12345678")]
 public void InvalidColorIsRejected(string value){Assert.Throws<ArgumentException>(()=>new OverlayAppearance(value,"112233","445566","778899"));}
 [Fact] public void DefaultPalettePreservesExistingPixels(){var a=OverlayAppearance.Default;foreach(var p in new[]{new Pixel(65,155,245,40,PixelKind.Friendly),new Pixel(240,90,70,40,PixelKind.Enemy),new Pixel(177,130,215,40,PixelKind.Balanced),new Pixel(240,225,155,192,PixelKind.Front)}){var q=a.Apply(p);Assert.Equal(p.R,q.R);Assert.Equal(p.G,q.G);Assert.Equal(p.B,q.B);Assert.Equal(p.A,q.A);}}
}
