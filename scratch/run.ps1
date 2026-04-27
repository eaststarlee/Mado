$src = Get-Content "c:\Users\admin\Documents\GitHub\Mado\scratch\RefactorStates.cs" -Raw
Add-Type -TypeDefinition $src
[Refactor]::Main()
