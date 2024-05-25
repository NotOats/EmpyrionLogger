## EmpyrionLogger
A simple mod to enable console logging with EGS

### Development Setup
To setup this project for development or custom builds:

- Clone the repo
```sh
    git clone https://github.com/NotOats/EmpyrionLogger
```

- Copy game assemblies
   1. Find the Dedicated Server steam install location
   1. Naviate to the managed assemblly folder
      This is typically found at `<Install_Folder>\Empyrion - Dedicated Server\DedicatedServer\EmpyrionDedicated_Data\Managed`
   1. Copy the following into a folder named `dependencies` in the project root.
      ```
      Mif.dll
      ModApi.dll
      UnityEngine.dll
      UnityEngine.CoreModule.dll
      Utils.dll
      ```