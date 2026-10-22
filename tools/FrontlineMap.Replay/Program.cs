using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using FrontlineMap.Core;
using FrontlineMap.Runtime;
using FrontlineMap.Diagnostics;

var argsMap=new Dictionary<string,string>();
for(int i=0;i<args.Length;i+=2){if(i+1>=args.Length)throw new ArgumentException("Expected named option and value");argsMap.Add(args[i],args[i+1]);}
if (argsMap.TryGetValue("--layered", out var layeredOption))
{
    if (layeredOption != "true" && layeredOption != "false") throw new ArgumentException("--layered expects true or false");
    if (layeredOption == "true") return FrontlineMap.Replay.LayeredReplay.Run(argsMap);
}
string fixtures=argsMap["--fixtures"], output=argsMap["--output"];
if(argsMap["--compare"]!="oracle,candidate")throw new ArgumentException("Supported comparison: oracle,candidate");
Directory.CreateDirectory(output);
using var tolerances=JsonDocument.Parse(File.ReadAllText(Path.Combine(fixtures,"tolerances.json")));
double absolute=tolerances.RootElement.GetProperty("absolute").GetDouble(),relative=tolerances.RootElement.GetProperty("relative").GetDouble();
var reports=new List<object>();bool allPass=true;
foreach(string file in Directory.GetFiles(fixtures,"*.snapshot.json").OrderBy(v=>v,StringComparer.Ordinal))
{
    using var document=JsonDocument.Parse(File.ReadAllText(file));var input=document.RootElement;
    if ((input.TryGetProperty("schema", out var schema) && schema.GetInt32()!=1) || input.TryGetProperty("model", out _))
        throw new NotSupportedException("Replay supports Legacy schema1 only. Layered schema2 exports contain observations and require local terrain; cannot compare as Legacy.");
    int n=input.GetProperty("resolution").GetInt32();double radius=input.GetProperty("radius").GetDouble();
    var bounds=input.GetProperty("bounds").EnumerateArray().Select(v=>v.GetDouble()).ToArray();
    var g=new MapGeometry(bounds[0],bounds[1],bounds[2],bounds[3],n,n);
    var stamps=input.GetProperty("stamps").EnumerateArray().Select(v=>new ObservationStamp(v.GetProperty("id").GetString(),v.GetProperty("side").GetString()=="friendly"?Side.Friendly:Side.Enemy,v.GetProperty("x").GetDouble(),v.GetProperty("z").GetDouble(),v.GetProperty("weight").GetDouble(),0,false)).ToArray();
    var settings=new ModelSettings(n,radius,5,.16,.01,1e-9,2,15);
    var clock=Stopwatch.StartNew();
    var snapshot=new ObservationSnapshot(new SnapshotIdentity(1,1,1,1,1),g,stamps,0,0,15);
    var timer=Stopwatch.StartNew();var oracle=ScalarField.Compute(snapshot,settings);double oracleMs=timer.Elapsed.TotalMilliseconds;
    timer.Restart();using var scheduler=new StepScheduler(()=>clock.Elapsed.TotalSeconds);
    scheduler.Begin(snapshot,settings,800,800);
    FieldResult candidate;
    do {scheduler.Step(clock.Elapsed.TotalSeconds+.002);if(clock.Elapsed.TotalSeconds>15)throw new TimeoutException("Fixture expired");}while(!scheduler.TryTakeCompleted(out candidate));
    double candidateMs=timer.Elapsed.TotalMilliseconds,maxError=0;int mismatches=0;
    var referencePixels=Rasterizer.Render(oracle,settings,800,800);
    bool pass=true;
    for(int i=0;i<g.Count;i++)
    {
        foreach(var pair in new[]{(oracle.Friendly[i],candidate.Field.Friendly[i]),(oracle.Enemy[i],candidate.Field.Enemy[i])})
        {double error=Math.Abs(pair.Item1-pair.Item2);maxError=Math.Max(maxError,error);if(!double.IsFinite(pair.Item2)||error>absolute+relative*Math.Abs(pair.Item1))pass=false;}
        if(referencePixels[i].Kind!=candidate.Pixels[i].Kind||referencePixels[i].A!=candidate.Pixels[i].A){mismatches++;pass=false;}
    }
    string id=Path.GetFileName(file);allPass&=pass;
    var rgba=new byte[g.Count*4];for(int i=0;i<g.Count;i++){var p=candidate.Pixels[i];rgba[i*4]=p.R;rgba[i*4+1]=p.G;rgba[i*4+2]=p.B;rgba[i*4+3]=p.A;}
    File.WriteAllBytes(Path.Combine(output,id+".rgba"),rgba);
    var report=new {fixture=id,fixture_sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))),status=pass?"pass":"fail",max_absolute_error=maxError,classification_or_alpha_mismatches=mismatches,cells=g.Count,observations=stamps.Length,oracle_tool_ms=oracleMs,candidate_tool_ms=candidateMs,allocation_ms=scheduler.Metrics.Milliseconds(WorkStage.Allocation),stamp_ms=scheduler.Metrics.Milliseconds(WorkStage.Stamp),raster_ms=scheduler.Metrics.Milliseconds(WorkStage.Raster),max_chunk_ms=scheduler.Metrics.MaximumChunkMilliseconds,rgba_sha256=Convert.ToHexString(SHA256.HashData(rgba))};
    reports.Add(report);Console.WriteLine($"{id}: {(pass?"PASS":"FAIL")} max_error={maxError:R}, mismatches={mismatches}");
}
if(reports.Count==0)throw new InvalidOperationException("No snapshot fixtures found");
File.WriteAllText(Path.Combine(output,"comparison.json"),JsonSerializer.Serialize(new {runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,platform=System.Runtime.InteropServices.RuntimeInformation.OSDescription,scope="tooling parity only; not Unity performance/freshness evidence; untuned candidate includes raster while oracle timing is field only, timings not comparable",status=allPass?"pass":"fail",fixtures=reports},new JsonSerializerOptions{WriteIndented=true}));
return allPass?0:1;
