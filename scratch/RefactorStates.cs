using System.IO;
using System.Text.RegularExpressions;

public class Refactor
{
    public static void Main()
    {
        string dir = @"c:\Users\admin\Documents\GitHub\Mado\Assets\Scripts\Character\Player\StateMachine\States";
        string[] files = Directory.GetFiles(dir, "*.cs");

        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            string className = Path.GetFileNameWithoutExtension(file);

            content = Regex.Replace(content, @"int\s+animBoolHash", "Mado.Character.Animation.PlayerAnimType animType");
            content = Regex.Replace(content, @",\s*animBoolHash\s*\)", ", animType)");

            string pattern1 = @"public\s+" + className + @"\s*\(\s*PlayerController\s+player\s*,\s*PlayerStateMachine\s+stateMachine\s*\)\s*:\s*base\s*\(\s*player\s*,\s*stateMachine\s*\)";
            string repl1 = "public " + className + "(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)";
            content = Regex.Replace(content, pattern1, repl1);

            string pattern2 = @"public\s+" + className + @"\s*\(\s*PlayerController\s+player\s*,\s*PlayerStateMachine\s+stateMachine\s*,\s*GrappleData\s+grappleData\s*\)\s*:\s*base\s*\(\s*player\s*,\s*stateMachine\s*\)";
            string repl2 = "public " + className + "(PlayerController player, PlayerStateMachine stateMachine, GrappleData grappleData, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)";
            content = Regex.Replace(content, pattern2, repl2);

            content = content.Replace("PlayerAnimID.Idle", "Mado.Character.Animation.PlayerAnimType.Idle");

            File.WriteAllText(file, content);
        }
    }
}
