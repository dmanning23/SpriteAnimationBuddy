rm *.nupkg
nuget pack .\SpriteAnimationBuddy.nuspec -IncludeReferencedProjects -Prop Configuration=Release
cp *.nupkg C:\Projects\Nugets\