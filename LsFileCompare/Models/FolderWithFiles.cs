using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LsFileCompare.Models
{
	public class FolderWithFiles
	{
		public FolderWithFiles()
		{
			Files = new List<FileWithDetails>();
		}
		public string Name { get; set; }
		public List<FileWithDetails> Files { get; set; }
	}

	public class FileWithDetails
	{
		public string FullName { get; set; }
		public string Name { get; set; }
		public long Size { get; set; }
		public string Reason { get; set; }
	}
}
