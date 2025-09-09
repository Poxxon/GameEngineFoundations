# Assignment 1 OpenGL Setup & First Render

## Library Used
I used OpenTK for the project. I ended up running this on my Mac since I couldn’t get it working properly through Visual Studio Community on Windows. I used the terminal to install .NET and needed SDKs alongside all the needed dependencies + OpenTK by using the following commands:

### To create the project and add OpenTK:
```bash
dotnet new console -f net8.0 -n WindowEngine

dotnet add package OpenTK
dotnet add package OpenTK.Windowing.Desktop
dotnet add package OpenTK.Graphics.OpenGL4
```

## How to run project
To run the program, you can :
* Clone the repo
* Make sure to have .NET 8 or above SDK installed
* In the project terminal run:
```bash
dotnet run
```