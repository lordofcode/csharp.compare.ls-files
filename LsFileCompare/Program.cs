using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using LsFileCompare.Models;

namespace LsFileCompare
{
	class Program
	{
		static void Main(string[] args)
		{
			string filelocation = ConfigurationManager.AppSettings["filelocation"];
			var externalDiskFiles = Directory.GetFiles(filelocation, ConfigurationManager.AppSettings["sourcematch"]);
			var localFiles = Directory.GetFiles(filelocation, ConfigurationManager.AppSettings["targetmatch"]);
			var debugResult = new StringBuilder();
			if (externalDiskFiles.Count() == 0 || localFiles.Count() == 0)
			{
				Console.WriteLine($"Error, missing source- and/or targetfiles: {externalDiskFiles.Count()} sourcefiles, {localFiles.Count()} targetfiles.");
				Console.ReadKey();
				return;
			}
			if (externalDiskFiles.Count() > 1 && externalDiskFiles.Count() != localFiles.Count())
			{
				Console.WriteLine($"Error, dismatch in number of source/target: {externalDiskFiles.Count()} sourcefiles, {localFiles.Count()} targetfiles.");
				Console.ReadKey();
				return;
			}
			var compareFileList = new List<KeyValuePair<string, string>>();
			if (externalDiskFiles.Count() == 1 && localFiles.Count() == 1)
			{
				compareFileList.Add(new KeyValuePair<string, string>(externalDiskFiles.First(), localFiles.First()));
			}
			else
			{
				try
				{
					foreach (var file in externalDiskFiles)
					{
						var targetfile = file.Replace(ConfigurationManager.AppSettings["matchsource"], ConfigurationManager.AppSettings["matchtarget"]);
						if (!File.Exists(targetfile))
						{
							throw new Exception($"{targetfile} does not exist!");
						}
						compareFileList.Add(new KeyValuePair<string, string>(file, targetfile));
					}
				}
				catch (Exception x)
				{
					Console.WriteLine($"Error connecting source- and targetfiles: {x.Message}.");
					Console.ReadKey();
					return;
				}
			}
			foreach (var compareItem in compareFileList)
			{
				var sourceLines = File.ReadAllLines(compareItem.Key);
				var sourceFolderWithFiles = LoadFoldersAndFiles(sourceLines);
				var localLines = File.ReadAllLines(localFiles.Where(rec => rec.EndsWith(compareItem.Value)).First());
				var localFolderWithFiles = LoadFoldersAndFiles(localLines);
				var compareResult = CompareFolders(sourceFolderWithFiles, localFolderWithFiles);
				if (compareResult.Count > 0)
				{
					Console.WriteLine("Found a mismatch:");
					foreach (var item in compareResult)
					{
						foreach (var debugFile in item.Files)
						{
							debugResult.AppendLine(debugFile.FullName);
							Console.WriteLine(debugFile.FullName + " : " + debugFile.Reason);
						}
					}
				}
				else
				{
					Console.WriteLine("OK, source and target are the same : " + compareItem.Key);
				}
			}
			File.WriteAllText($"{ConfigurationManager.AppSettings["filelocation"]}error_output.txt", debugResult.ToString());
			Console.WriteLine("Done!");
			Console.ReadKey();
		}

		private static List<FolderWithFiles> LoadFoldersAndFiles(string[] data)
		{
			var allFolders = new List<FolderWithFiles>();
			var folder = new FolderWithFiles() { Name = "" };
			foreach (var line in data)
			{
				if (string.IsNullOrEmpty(line))
				{
					continue;
				}
				if (line.StartsWith("total"))
				{
					continue;
				}
				var matchfolderValue = ConfigurationManager.AppSettings["foldermatchvalue"];
				if (line.Contains(matchfolderValue))
				{
					allFolders.Add(folder);
					var subfolder = matchfolderValue;
					if (matchfolderValue.Contains("/"))
					{
						subfolder = matchfolderValue.Substring(matchfolderValue.IndexOf("/"));
						if (subfolder.Length > 1)
						{
							subfolder = subfolder.Substring(1);
						}
						if (subfolder.Length == 0)
						{
							subfolder = matchfolderValue;
						}
					}
					folder = new FolderWithFiles() { Name = line.Substring(line.IndexOf(subfolder)) };

					continue;
				}
				var f = ParseLineToFileWithDetails(line);
				if (f == null)
				{
					continue;
				}
				folder.Files.Add(f);
			}
			allFolders.Add(folder);

			for (var m = 0; m < allFolders.Count; m++)
			{
				var f = allFolders[m];
				f.Name = f.Name.TrimEnd(new char[] { ':', '/' });
				f.Name += "/";

				foreach (var ff in f.Files)
				{
					ff.FullName = f.Name + ff.Name;
				}

				if (m == 1)
				{
					allFolders[0].Name = allFolders[1].Name.Substring(0, allFolders[1].Name.TrimEnd(new char[] { '/' }).LastIndexOf('/') + 1);
					foreach (var ff in allFolders[0].Files)
					{
						ff.FullName = allFolders[0].Name + ff.Name;
					}
				}
			}
			return allFolders;
		}

		private static FileWithDetails ParseLineToFileWithDetails(string line)
		{
			var file = new FileWithDetails();
			line = System.Text.RegularExpressions.Regex.Replace(line, @"([\s]){2,}", " ");
			var parts = line.Split(new char[] { ' ' });
			if (parts[0].Substring(0, 1) == "d")
			{
				return null; ;
			}
			file.Size = long.Parse(parts[4]);
			file.Name = "";
			for (var m = 8; m < parts.Length; m++)
			{
				file.Name += parts[m] + " ";
			}
			file.Name = file.Name.Trim();
			return file;
		}

		private static List<FolderWithFiles> CompareFolders(List<FolderWithFiles> source, List<FolderWithFiles> destination)
		{
			var result = new List<FolderWithFiles>();
			foreach (var item in source)
			{
				var destinationFolder = destination.FirstOrDefault(rec => rec.Name == item.Name);
				if (destinationFolder == null)
				{
					foreach (var reasonitem in item.Files)
					{
						reasonitem.Reason = "file does not exist";
					}
					result.Add(item);
				}
				else
				{
					foreach (var file in item.Files)
					{
						var destinationFile = destinationFolder.Files.FirstOrDefault(rec => rec.Name == file.Name);
						if (destinationFile == null)
						{
							file.Reason = "file does not exist";
							result.Add(new FolderWithFiles() { Name = item.Name, Files = new List<FileWithDetails>() { file } });
						}
						else
						{
							if (file.Size != destinationFile.Size)
							{
								file.Reason = $"mismatch by size ({file.Size} vs {destinationFile.Size})";
								result.Add(new FolderWithFiles() { Name = item.Name, Files = new List<FileWithDetails>() { file } });
							}
						}
					}
				}
			}
			return result;
		}

	}

}
