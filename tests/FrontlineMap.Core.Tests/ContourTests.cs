using FrontlineMap.Core;
using Xunit;

public class ContourTests
{
    // Literal samples make changes to interpolation, topology and eligibility observable.
    private static InfluenceBuffers Field(int columns, int rows, params double[] differences)
    {
        var f = new InfluenceBuffers(new MapGeometry(100, -200, 900, 70, columns, rows));
        for (int i = 0; i < differences.Length; i++)
        {
            f.Friendly[i] = 10 + differences[i] / 2;
            f.Enemy[i] = 10 - differences[i] / 2;
        }
        return f;
    }
    private static void Point(ContourPoint p, double u, double v)
    {
        Assert.Equal(u, p.U, 12); Assert.Equal(v, p.V, 12);
    }
    private static bool Joins(ContourSegment s, double au, double av, double bu, double bv) =>
        (Near(s.A, au, av) && Near(s.B, bu, bv)) || (Near(s.B, au, av) && Near(s.A, bu, bv));
    private static bool Near(ContourPoint p, double u, double v) => Math.Abs(p.U-u)<1e-12 && Math.Abs(p.V-v)<1e-12;

    [Fact] public void VerticalCrossingInterpolatesBetweenCellCenters()
    {
        var s = Assert.Single(FrontContours.Extract(Field(2, 2, -1, 3, -1, 3), 1, 1e-9));
        Assert.True(Joins(s, .375, .25, .375, .75));
        Assert.Equal(1, s.Confidence);
    }
    [Fact] public void HorizontalCrossingUsesRectangularRowsWithoutMirroring()
    {
        var s = FrontContours.Extract(Field(3, 2, -3, -3, -3, 1, 1, 1), 1, 1e-9);
        Assert.Equal(2, s.Count);
        Assert.Contains(s, p => Joins(p, 1.0/6, .625, .5, .625));
        Assert.Contains(s, p => Joins(p, .5, .625, 5.0/6, .625));
    }
    [Theory]
    [InlineData(4, -1, -1, 4, true)]
    [InlineData(1, -4, -4, 1, false)]
    [InlineData(1, -1, -1, 1, true)]
    public void SaddlesUseAsymptoticConnectivityAndDeterministicTie(double a, double b, double d, double c, bool bottomRight)
    {
        var f = Field(2, 2, a, b, d, c);
        var s = FrontContours.Extract(f, 1, 1e-9);
        Assert.Equal(2, s.Count);
        var bottom = s.Single(v => Math.Abs(v.A.V-.25)<1e-12 || Math.Abs(v.B.V-.25)<1e-12);
        Assert.Equal(bottomRight, Math.Abs(bottom.A.U-.75)<1e-12 || Math.Abs(bottom.B.U-.75)<1e-12);
        var again = FrontContours.Extract(f, 1, 1e-9);
        for (int i=0;i<s.Count;i++) { Point(again[i].A,s[i].A.U,s[i].A.V); Point(again[i].B,s[i].B.U,s[i].B.V); }
    }
    [Fact] public void BalancedPlateausAndNearZeroPlateausHaveNoLine()
    {
        Assert.Empty(FrontContours.Extract(Field(2,2,0,0,0,0),1,1e-9));
        Assert.Empty(FrontContours.Extract(Field(2,2,-1e-10,1e-10,-1e-10,1e-10),1,1e-9));
    }
    [Fact] public void ExactZeroColumnProducesOneNondegenerateLine()
    {
        var s = Assert.Single(FrontContours.Extract(Field(3,2,-1,0,1,-1,0,1),1,1e-9));
        Assert.True(Joins(s,.5,.25,.5,.75));
    }
    [Fact] public void ZeroCornerTouchDoesNotCreateZeroLengthLine()
    {
        Assert.Empty(FrontContours.Extract(Field(2,2,0,-1,-1,-1),1,1e-9));
    }
    [Fact] public void SupportIsInterpolatedAtEndpointsRatherThanRequiringAllCorners()
    {
        var f = Field(2,2,-1,3,-1,3);
        for(int i=0;i<4;i++) { f.Friendly[i] = i%2==0 ? 0 : 4; f.Enemy[i]=1; }
        Assert.Single(FrontContours.Extract(f,1,1e-9));
        Assert.Empty(FrontContours.Extract(f,1.001,1e-9));
    }
    [Fact] public void OneUnsupportedEndpointRejectsEntireSegment()
    {
        var f = Field(2,2,-1,1,-1,1);
        f.Friendly[0]=0; f.Enemy[0]=1; f.Friendly[1]=1; f.Enemy[1]=0;
        Assert.Empty(FrontContours.Extract(f,1,1e-9));
    }
    [Fact] public void OneSidedSupportBoundaryIsNeverAFront()
    {
        var f = new InfluenceBuffers(new MapGeometry(0,0,1,1,2,2));
        f.Friendly[1]=5; f.Friendly[3]=5;
        Assert.Empty(FrontContours.Extract(f,.1,1e-9));
    }
    [Fact] public void ConfidenceUsesInterpolatedEndpointMinimumAndInclusiveThreshold()
    {
        var f=Field(2,2,-1,3,-1,3); var confidence=new[]{.4,.8,.8,.8};
        var s=Assert.Single(FrontContours.Extract(f,1,1e-9,confidence,.5));
        Assert.Equal(.5,s.Confidence,12);
        Assert.Empty(FrontContours.Extract(f,1,1e-9,confidence,.5001));
    }
    [Theory] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(-1)]
    public void InvalidSampleCannotEmitGeometry(double invalid)
    {
        var f=Field(2,2,-1,1,-1,1); f.Friendly[0]=invalid;
        Assert.Empty(FrontContours.Extract(f,1,1e-9));
    }
    [Fact] public void VeryLargeFiniteSamplesDoNotOverflowInterpolationOrSaddle()
    {
        var f=Field(2,2,-1,1,-1,1);
        f.Friendly[0]=f.Friendly[2]=0; f.Enemy[0]=f.Enemy[2]=1e308;
        f.Friendly[1]=f.Friendly[3]=1e308; f.Enemy[1]=f.Enemy[3]=0;
        var s=Assert.Single(FrontContours.Extract(f,1,0));
        Assert.True(Joins(s,.5,.25,.5,.75));
    }
    [Fact] public void InvalidConfidenceAndSettingsAreConservative()
    {
        var f=Field(2,2,-1,1,-1,1);
        Assert.Empty(FrontContours.Extract(f,1,0,new[]{double.NaN,1,1,1}));
        Assert.Throws<ArgumentException>(()=>FrontContours.Extract(f,1,0,new double[1]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrontContours.Extract(f,-1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrontContours.Extract(f,1,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrontContours.Extract(f,1,0,null,1.1));
    }
    [Fact] public void ExtractionStepsBoundWorkAndPreserveFullResult()
    {
        const int columns=33, rows=17;
        var f=Field(columns,rows);
        for(int z=0;z<rows;z++) for(int x=0;x<columns;x++)
        { f.Friendly[z*columns+x]=((x+z)%2==0)?11:9; f.Enemy[z*columns+x]=10; }
        var expected=FrontContours.Extract(f,1,0);
        Assert.Equal(1024,expected.Count);
        var output=new List<ContourSegment>();
        using var steps=FrontContours.ExtractSteps(f,1,0,output).GetEnumerator();
        Assert.Empty(output);
        Assert.True(steps.MoveNext());
        Assert.InRange(steps.Current,1,128);
        Assert.Equal(256,output.Count);
        int work=steps.Current;
        while(steps.MoveNext()) { Assert.InRange(steps.Current,1,128); work+=steps.Current; }
        Assert.Equal(512,work);
        Assert.Equal(expected.Count,output.Count);
        for(int i=0;i<expected.Count;i++)
        {
            Point(output[i].A,expected[i].A.U,expected[i].A.V);
            Point(output[i].B,expected[i].B.U,expected[i].B.V);
            Assert.Equal(expected[i].Confidence,output[i].Confidence);
        }
    }
    [Fact] public void FirstYieldPrecedesReadingLateCellsEvenWithoutContours()
    {
        var f=Field(2,258);
        var output=new List<ContourSegment>();
        using var steps=FrontContours.ExtractSteps(f,1,0,output).GetEnumerator();
        Assert.True(steps.MoveNext());
        Assert.InRange(steps.Current,1,128);
        Assert.Empty(output);
        // Late samples are still unprocessed after the first bounded step.
        for(int z=256;z<258;z++)
        { f.Friendly[z*2]=9; f.Enemy[z*2]=10; f.Friendly[z*2+1]=11; f.Enemy[z*2+1]=10; }
        while(steps.MoveNext()) Assert.InRange(steps.Current,1,128);
        Assert.NotEmpty(output);
        Assert.Contains(output,s=>Joins(s,.5,256.5/258,.5,257.5/258));
    }

}
