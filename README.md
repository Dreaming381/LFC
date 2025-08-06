# LFC

This is the Latios Framework Community Unity Project. It is a space for the
community to collaborate and explore the Latios Framework features together.

**Q: Is this supposed to be a game?**

A: That’s up to the community to decide.

**Q: What am I allowed to do in it?**

A: You can add any assets that you made yourself. And you can modify the project
in any way except for the following:

-   Changing the editor version
-   Changing packages
    -   Exception: You are free and encouraged to update git packages.

Once you are done with your changes, feel free to make a pull request.

**Q: What if I want to use an asset pack?**

A: Requests to add asset packs to the can be submitted as GitHub issues or on
the community discord. You can also use these channels to request a new branch
targeting prerelease packages, or request changes to the editor version or
packages installed.

**Q: What if I want to modify the framework or add-ons packages?**

A: Simply embed the packages into the Packages folder, and ensure the folder
names start with *com.latios.latiosframework* so that they are properly ignored
by git.

**Q: Why is the Latios Framework version out of date?**

A: The packages-lock.json file is git-ignored so that you can experiment with
local versions of the framework and add-on packages. This also means the project
always uses the latest stable release versions of the packages on a fresh clone.
However, after that point, you need to update the packages for yourself.

**Q: What is the license of this repo?**

A: Unity Companion License, same as the framework.
