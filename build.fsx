// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System

let buildDir = "build/"
let testDir  = "test/"
let tempDir  = "temp/"

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package 
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project             = "Metricano"
let cloudWatchProject   = "Metricano.CloudWatch"
let postSharpProject    = "Metricano.PostSharpAspects"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary           = "Agent-based F# library for collecting, aggregating and publishing metrics"
let cloudWatchSummary = "Publisher for Metricano that publishes metrics to Amazon CloudWatch"
let postSharpSummary  = "PostSharp aspects for recording method execution count and time with Metricano"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description           = "Agent-based F# library for collecting, aggregating and publishing metrics"
let cloudWatchDescription = "Publisher for Metricano that publishes metrics to Amazon CloudWatch"
let postSharpDescription  = "PostSharp aspects for recording method execution count and time with Metricano"

// List of author names (for NuGet package)
let authors = [ "Yan Cui" ]
// Tags for your project (for NuGet package)
let tags = "F# fsharp aws amazon cloudwatch"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = ["tests/*/bin/*/Metricano*Tests*.dll"]

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/theburningmonk"
// The name of the project on GitHub
let gitName = "Metricano"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  CreateFSharpAssemblyInfo "src/Metricano/AssemblyInfo.fs"
      [ Attribute.Title         project
        Attribute.Product       project
        Attribute.Description   summary
        Attribute.Version       release.AssemblyVersion
        Attribute.FileVersion   release.AssemblyVersion
        Attribute.InternalsVisibleTo "Metricano.Tests" ]

  CreateFSharpAssemblyInfo "src/Metricano.CloudWatch/AssemblyInfo.fs"
      [ Attribute.Title         cloudWatchProject
        Attribute.Product       cloudWatchProject
        Attribute.Description   cloudWatchSummary
        Attribute.Version       release.AssemblyVersion
        Attribute.FileVersion   release.AssemblyVersion ]

  CreateCSharpAssemblyInfo "src/Metricano.PostSharpAspects/Properties/AssemblyInfo.cs"
      [ Attribute.Title         postSharpProject
        Attribute.Product       postSharpProject
        Attribute.Description   postSharpSummary
        Attribute.Version       release.AssemblyVersion
        Attribute.FileVersion   release.AssemblyVersion ]
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "RestorePackages" RestorePackages

Target "Clean" (fun _ ->
    CleanDirs [ buildDir; testDir; tempDir ]
)

Target "CleanDocs" (fun _ ->
    CleanDirs [ "docs/output" ]
)

// --------------------------------------------------------------------------------------
// Build library & test project

let files includes = 
  { BaseDirectory = __SOURCE_DIRECTORY__
    Includes = includes
    Excludes = [] } 

Target "Build" (fun _ ->
    files [ "src/Metricano/Metricano.fsproj"
            "src/Metricano.CloudWatch/Metricano.CloudWatch.fsproj"
            "src/Metricano.PostSharpAspects/Metricano.PostSharpAspects.csproj"
            "tests/Metricano.Tests/Metricano.Tests.fsproj"
            "tests/Metricano.CloudWatch.Tests/Metricano.CloudWatch.Tests.fsproj" ]
    |> MSBuildRelease buildDir "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target "RunTests" (fun _ ->
    ActivateFinalTarget "CloseTestRunner"

    { BaseDirectory = __SOURCE_DIRECTORY__
      Includes = testAssemblies
      Excludes = [] } 
    |> NUnit (fun p ->
        { p with
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 5.
            OutputFile = "test/TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let description = description.Replace("\r", "")
                                 .Replace("\n", "")
                                 .Replace("  ", " ")

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "nuget"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [ ] })
        ("nuget/" + project + ".nuspec")

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = cloudWatchProject
            Summary = cloudWatchSummary
            Description = cloudWatchDescription
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "nuget"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = 
                [ "Metricano", release.NugetVersion
                  "AWSSDK",    GetPackageVersion "packages" "AWSSDK" ] })
        ("nuget/" + cloudWatchProject + ".nuspec")

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = postSharpProject
            Summary = postSharpSummary
            Description = postSharpDescription
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "nuget"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = 
                [ "Metricano", release.NugetVersion
                  "PostSharp", GetPackageVersion "packages" "PostSharp" ] })
        ("nuget/" + postSharpProject + ".nuspec")
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let ghPages      = "gh-pages"
    let ghPagesLocal = "temp/gh-pages"
    Repository.clone "temp" (gitHome + "/" + gitName + ".git") ghPages
    Branches.checkoutBranch ghPagesLocal ghPages
    fullclean ghPagesLocal
    CopyRecursive "docs/output" ghPagesLocal true |> printfn "%A"
    CommandHelper.runSimpleGitCommand ghPagesLocal "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
    CommandHelper.runSimpleGitCommand ghPagesLocal cmd |> printfn "%s"
    Branches.push ghPagesLocal
)

Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "All"

"All" 
//  ==> "CleanDocs"
//  ==> "GenerateDocs"
//  ==> "ReleaseDocs"
  ==> "NuGet"
  ==> "Release"

RunTargetOrDefault "All"
