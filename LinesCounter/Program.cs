using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace LinesCounter
{
    public class Solution
    {
        public readonly string FileName;

        public readonly string Name;

        public readonly List<Project> Projects;

        public long LinesCount
        {
            get { return Projects.Sum(project => project.LinesCount); }
        }

        public long Size
        {
            get { return Projects.Sum(project => project.Size); }
        }

        public Solution(string fileName)
        {
            FileName = fileName;
            Name = Path.GetFileNameWithoutExtension(fileName);
            Projects = new List<Project>();

            var solutionPath = Path.GetDirectoryName(Path.GetFullPath(fileName));
            var re = new Regex("^Project.*=.*\"(.*)\",\\s*\"(.*)\",", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            try
            {
                foreach (var line in File.ReadAllLines(fileName))
                {
                    var match = re.Match(line);
                    if (match.Groups.Count < 3) continue;
                    var projectName = match.Groups[1].ToString();
                    var projectFile = String.Format("{0}\\{1}", solutionPath, match.Groups[2]);
                    var ext = Path.GetExtension(projectFile).ToLower();
                    if (ext.Equals(".csproj")) Projects.Add(new Project(projectFile, projectName));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error parsing solution: {0}", FileName), ex);
            }
        }
    }


    public class Project
    {
        public readonly string FileName;

        public readonly string Name;

        public readonly List<SourceFile> SourceFiles;

        public long LinesCount
        {
            get { return SourceFiles.Sum(file => file.LinesCount); }
        }

        public long Size
        {
            get { return SourceFiles.Sum(file => file.Size); }
        }

        public Project(string fileName)
            : this(fileName, Path.GetFileNameWithoutExtension(fileName)) { }

        public Project(string fileName, string name)
        {
            FileName = fileName;
            Name = name;
            SourceFiles = new List<SourceFile>();

            var projectPath = Path.GetDirectoryName(fileName);
            var reader = XmlReader.Create(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            while(reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("Compile"))
                {
                    SourceFiles.Add(new SourceFile(String.Format("{0}\\{1}", projectPath, reader.GetAttribute("Include"))));
                }
            }
        }
    }

    public class SourceFile
    {
        public readonly string FileName;

        public readonly long LinesCount;

        public readonly long Size;

        public SourceFile(string fileName)
        {
            FileName = fileName;
            try
            {
                LinesCount = File.ReadAllLines(fileName).Length;
                Size = new FileInfo(fileName).Length;
            }
            catch(Exception ex)
            {
                throw new Exception(String.Format("Error reading source file: {0}", FileName), ex);
            }
        }
    }

    class Program
    {
        static void ProcessResult(string mode, long linesCount, long size, string statFile)
        {
            var info = new List<string>();
            if (mode == null || mode.Contains("l")) info.Add(linesCount.ToString());
            if (mode != null && mode.Contains("s")) info.Add(size.ToString());

            Console.WriteLine(String.Join(";", info));

            if (statFile != null)
            {
                try
                {
                    info.Insert(0, DateTime.Now.ToString("yyyy-MM-dd"));
                    File.AppendAllText(statFile, String.Format("{0}{1}", String.Join(";", info), Environment.NewLine));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Format("Error saving to {0}: {1}", statFile, ex.Message));
                }
            }
        }

        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Source lines counter for Visual Studio projects");
                Console.WriteLine();
                Console.WriteLine("Usage: lcnt [options] <filename> [logfile]");
                Console.WriteLine("  [options]  - /l to get number of LOCs, /s for source files size or /ls for both");
                Console.WriteLine("  <filename> - *.sln or *.csproj file to process");
                Console.WriteLine("  [logfile]  - optional log file to append calculated data and timestamp");
                Console.WriteLine();
                return;
            }

            var mode = (args.Length > 1) ? args[0] : String.Empty;

            if(!(new Regex("^/[ls]{1,2}$").IsMatch(mode)))
            {
                Console.WriteLine("Incorrect options.");
                return;
            }

            var fileName = Path.GetFullPath(args[(args.Length > 1) ? 1 : 0]);
            var statFile = (args.Length > 2) ? Path.GetFullPath(args[2]) : null;

            if (!File.Exists(fileName))
            {
                Console.WriteLine("File doesn't exists.");
                return;
            }

            var ext = Path.GetExtension(fileName).ToLower();
            if (ext.EndsWith(".sln"))
            {
                var solution = new Solution(fileName);
                ProcessResult(mode, solution.LinesCount, solution.Size, statFile);
            }
            else if(ext.EndsWith(".csproj"))
            {
                var project = new Project(fileName);
                ProcessResult(mode, project.LinesCount, project.Size, statFile);
            }
            else
            {
                Console.WriteLine("The file type is not supported.");
                return;
            }
        }
    }
}
