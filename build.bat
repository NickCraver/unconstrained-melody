@ECHO OFF
ECHO Building Constraint Changer for IL re-writes...
dotnet publish ConstraintChanger -f netcoreapp1.0 -r win7-x64 -c Release -o Published
ECHO Building UnconstrainedMelody...
dotnet build UnconstrainedMelody -c Release

ECHO Illustrating boom directory (since the build hides it):
cd UnconstrainedMelody\bin\Release\netstandard2.0
..\..\..\..\ConstraintChanger\Published\ConstraintChanger.exe

ECHO Returning to home
cd ..\..\..\..\