using JDNext;

if (args.Length < 3)
{
    Console.WriteLine("Usage: Stargate <Base Bundle> <Map folder> <Output folder>");
    return;
}

string BaseBundle = args[0];
string MapPath = args[1];
string OutputBundle = args[2];

MapPackage map = new MapPackage(BaseBundle, MapPath);
map.ReplaceBundle(OutputBundle);
