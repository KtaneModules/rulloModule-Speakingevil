using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class RulloScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMSelectable[] buttons;
    public Renderer[] brends;
    public Renderer[] brings;
    public Renderer[] checkrends;
    public Material[] mats;
    public Material[] litmats;
    public TextMesh[] blabels;
    public TextMesh[] checklabels;
    public GameObject matstore;

    private int[,] nums = new int[6,6];
    private bool[,] not = new bool[6,6];
    private int[] sums = new int[12];
    private bool[,] off = new bool[6,6];
    private bool[,] locked = new bool[6,6];
    private IEnumerator press;
    private bool held;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        foreach(KMSelectable b in buttons)
        {
            int i = Array.IndexOf(buttons, b);
            b.OnInteract += delegate () 
            {
                if (!moduleSolved)
                {
                    if (i == 36) Check();
                    else
                    {
                        press = Press(i);
                        StartCoroutine(press);
                        held = false;
                        b.AddInteractionPunch(0.5f);
                    }
                }
                return false;
            };
            b.OnInteractEnded += delegate ()
            {
                if (!moduleSolved && i != 36)
                {
                    StopCoroutine(press);
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, b.transform);
                    if (!held && !locked[i / 6, i % 6])
                    {
                        off[i / 6, i % 6] ^= true;
                        brends[i].material = mats[off[i / 6, i % 6] ? 1 : 0];
                        blabels[i].color = off[i / 6, i % 6] ? new Color32(128, 128, 128, 255) : new Color32(102, 0, 134, 255);
                    }
                }
            };
        }
        for (int k = 0; k < Random.Range(2, 4); k++)
        {
            int[][] shuffle = new int[2][] { new int[6], new int[6] };
            for(int i = 0; i < 2; i++)
                shuffle[i] = new int[6] { 0, 1, 2, 3, 4, 5 }.Shuffle();
            for (int i = 0; i < 6; i++)
                not[shuffle[0][i], shuffle[1][i]] = true;
        }
        for (int i = 0; i < 36; i++)
        {
            int r = Random.Range(1, 10);
            nums[i / 6, i % 6] = r;
            blabels[i].text = r.ToString();
        }
        string[][] loggrid = new string[6][] { new string[6], new string[6], new string[6], new string[6], new string[6], new string[6] };
        string[] sol = new string[6];
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                sums[i] += not[j, i] ? 0 : nums[j, i];
                sums[i + 6] += not[i, j] ? 0 : nums[i, j];
                loggrid[i][j] = nums[i, j].ToString();
                sol[i] += ' ';
                sol[i] += not[i, j] ? '\u25cf' : '\u25cb';
            }
            checklabels[i].text = (sums[i] < 10 ? "0" : "") + sums[i].ToString();
            checklabels[i + 6].text = (sums[i + 6] < 10 ? "0" : "") + sums[i + 6].ToString();
        }
        Debug.LogFormat("[Rullo #{0}] The grid of digits is:\n[Rullo #{0}] {1}", moduleID, string.Join("\n[Rullo #" + moduleID + "] ", loggrid.Select(i => string.Join(" ", i)).ToArray()));
        Debug.LogFormat("[Rullo #{0}] The required column sums are: {1}", moduleID, string.Join(", ", sums.Where((x, i) => i < 6).Select(i => i.ToString()).ToArray()));
        Debug.LogFormat("[Rullo #{0}] The required row sums are: {1}", moduleID, string.Join(", ", sums.Where((x, i) => i > 5).Select(i => i.ToString()).ToArray()));
        Debug.LogFormat("[Rullo #{0}] One possible solution is:\n[Rullo #{0}] {1}", moduleID, string.Join("\n[Rullo #" + moduleID + "] ", sol));
        matstore.SetActive(false);
    }

    private IEnumerator Press(int i)
    {
        if (i != 36)
        {
            yield return new WaitForSeconds(0.5f);
            held = true;
            locked[i / 6, i % 6] ^= true;
            Audio.PlaySoundAtTransform("Flag", transform);
            brings[i].material = litmats[locked[i / 6, i % 6] ? 1 : 0];
        }
        yield return null;
    }

    private void Check()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[36].transform);
        int[] calcsum = new int[12];
        bool[] checksums = new bool[12];
        for (int j = 0; j < 6; j++)
        {
            for (int k = 0; k < 6; k++)
            {
                calcsum[j] += off[k, j] ? 0 : nums[k, j];
                calcsum[j + 6] += off[j, k] ? 0 : nums[j, k];
            }
            checksums[j] = calcsum[j] == sums[j];
            checksums[j + 6] = calcsum[j + 6] == sums[j + 6];
            checkrends[j].material = litmats[checksums[j] ? 2 : 3];
            checkrends[j + 6].material = litmats[checksums[j + 6] ? 2 : 3];
        }
        Debug.LogFormat("[Rullo #{0}] The submitted column sums are: {1}", moduleID, string.Join(", ", calcsum.Where((x, i) => i < 6).Select(i => i.ToString()).ToArray()));
        Debug.LogFormat("[Rullo #{0}] The submitted row sums are: {1}", moduleID, string.Join(", ", calcsum.Where((x, i) => i > 5).Select(i => i.ToString()).ToArray()));
        if (checksums.Contains(false))
            module.HandleStrike();
        else
        {
            moduleSolved = true;
            module.HandlePass();
            foreach (Renderer r in brings)
                r.material = litmats[0];
            foreach (TextMesh r in blabels.Union(checklabels))
                r.text = "\u2713";
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} toggle <a-f><1-6> [Press buttons] | !{0} flag <a-f><1-6> [Holds buttons] | [Chain toggles or flags by separating the coordinates with spaces] | !{0} submit";
#pragma warning restore 414
    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if(command == "submit")
        {
            yield return null;
            yield return "solve";
            yield return "strike";
            buttons[36].OnInteract();
        }
        else
        {
            string[] commands = command.Replace(",", "").Split();
            int[,] prompts = new int[command.Length - 1, 2];
            if(commands[0] != "toggle" && commands[0] != "flag")
            {
                yield return "sendtochaterror Invalid command: " + commands[0];
                yield break;
            }
            bool flag = commands[0] == "flag";
            for(int i = 1; i < commands.Length; i++)
            {
                if(commands[i].Length != 2 || !"abcdef".Contains(commands[i][0].ToString()) || !"123456".Contains(commands[i][1]))
                {
                    yield return "sendtochaterror Invalid command: " + commands[i];
                    yield break;
                }
                else
                {
                    for (int j = 0; j < 2; j++)
                        prompts[i - 1, j] = "abcdef123456".IndexOf(commands[i][j].ToString()) - (6 * j);
                }
            }
            for(int i = 0; i < commands.Length - 1; i++)
            {
                int b = (6 * prompts[i, 1]) + prompts[i, 0];
                yield return null;
                buttons[b].OnInteract();
                yield return new WaitForSeconds(flag ? 0.6f : 0.1f);
                buttons[b].OnInteractEnded();
            }
        }
    }
}
