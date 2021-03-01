# csharp.compare.ls-files

When I needed to recover files from a broken Western Digital external disk, I needed to know if I copied all files and folders to a save place.

So I compared the output of 2 textfiles, created with the commands:

ls -l -R /Volumes/Elements/sourcefolder > externaldisk.txt

ls -l -R /Users/dirk/klantenmap > lokaal.txt

Because sometimes the files seemed to be copied, but something went wrong, the copy was 0 kB.

I had to remove the file and try it again. In the end around 3.500 files were rescued.

My (Dutch) summary of this story can be found on:

https://www.durkotheek.nl/western-digital-elements-externe-schijf-van-1tb-heeft-kuren-help
