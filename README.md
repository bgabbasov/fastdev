# How to start

1. Clone the repository
2. Configure images store location in appsettings.json (by default uploaded images are saved into C:\fastdev\images directory)
3. Build the solution
4. Start web server (default hist address is http://localhost:1972)
5. Open terminal and go to <solution folder>/src/FastDev.Client/bin/Debug/netcoreapp1.0
6. Execute dotnet FastDev.Client.dll
7. Follow instructions on the screen
8. Go to http://localhost:1972 and browse uploaded A objects

# How it works

Images are transfered by multipart/form-data request.

Parameters should have following names guid[rowNo], file1[rowNo], file2[rowNo], file3[rowNo].
It is required that parameters belonging to the same row do not have any other rows between them in the request.

Maximum size of request is 2GB.

Having bigger size of files is inappropriate, because HTTP protocol does not support 
"partial" responses, which means that it will be hard to track upload progress.
If it is required to upload files more than 2GB, they should be split into chunks.
The natural way to do it is io split into chunks by objects, so that each chunk contains
some amount of A objects.

Assumption behind this is that the objects can be treated as separate objects and it is not needed
to process them in a scope of one transction.
