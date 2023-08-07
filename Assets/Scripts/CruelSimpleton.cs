using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

/*
 * TO DO: 
 * -AUTO SOLVER
 * --Unicorn
 * --Rule 7
 * --Rule 8
 * --Rule 9
 */

public class CruelSimpleton : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable topLeftSection;
    public KMSelectable bottomLeftSection;
    public KMSelectable bottomRightSection;
    public KMSelectable statusLightButton;
    public KMSelectable blueButton;

    public TextMesh buttonText;

    public AudioClip dashSound;
    public AudioClip dotSound;
    public AudioClip breakSound;

    public AudioClip moduleSolveSound;
    public AudioClip clearMorseSound;
    public AudioClip stageClearSound;


    private bool dashSoundPlayed;
    private bool dotSoundPlayed;
    private bool clearSoundPlayed;
    //tells which rule the user is on for the unicorn
    private int unicornRuleNum;

    private float initialBombTime;

    private string rule3Answer;

    private List<string> rule3Input;

    private List<List<string>> rule2Input;

    private int rule7Answer;

    private List<int> rule8Answer;

    private List<int> rule8Input;

    //time the user started holding down and releasing the button for rule 4
    private float rule4StartingTime;
    private float rule4EndingTime;

    private bool rule4Started;

    //number of time button has been pressed for rule 5
    private int buttonPressedNum;

    //tells if the user started pressing the button for rule 5
    private bool rule5Started;


    private float timeOffset;

    //if the status light being help
    private bool holdingStatus;

    //tells if the input is a dash or dot
    private int dashOrDot;

    private int dashThreshold;
    private int dotThreshold;
    private int breakThreshold;
    private int morseSubmitThreshold;

    private int rule5Answer = 10;


    private int rule2CurrentIndex;

    //tells if the module is submitting the morse code answer
    private int submitting;



    private bool mouseDown = false;


    //time when the user pressed/held the blue button
    private int bluePressTime;
    private int currentTime;
    private int lastTime;

    private enum Event
    {
        MouseUp,
        MouseDown,
        Tick
    }

    private bool unicorn;
    private bool rule1;
    private bool rule2;
    private bool rule3;
    private bool rule4;
    private bool rule5;

    private List<Event> rule1Inputs;

    private List<Event> rule1Answer;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    //fixes a bug where two strikes are given. One from rule 7 and another from rule 6,
    //rule 6 shoudln't apply
    private bool strikeAlreadyGiven;

    Dictionary<string, string> morse = new Dictionary<string, string>()
    {
        { ".-", "A" },
        { "-...", "B"},
        { "-.-.", "C" },
        { "-..", "D" },
        { ".", "E" },
        { "..-.", "F" },
        { "--.", "G" },
        { "....", "H"},
        { "..", "I" },
        { ".---", "J"},
        { "-.-", "K" },
        { ".-..", "L"},
        { "--", "M"},
        {"-.", "N"},
        { "---", "O"},
        { ".--.", "P"},
        { "--.-", "Q"},
        { ".-.", "R"},
        { "...", "S"},
        { "-","T"},
        { "..-", "U"},
        { "...-", "V"},
        { ".--", "W"},
        { "-..-", "X"},
        { "-.--", "Y"},
        { "--..", "Z"},
        { "-----", "0"},
        { ".----", "1"},
        { "..---", "2"},
        { "...--", "3"},
        { "....-", "4"},
        { ".....", "5"},
        { "-....", "6"},
        { "--...", "7"},
        {"---..", "8"},
        { "----.", "9"}
    };

    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        blueButton.OnInteract += delegate () { SectionPress(blueButton); return false; };

        blueButton.OnInteract += delegate () { BlueButtonPress(); return false; };

        blueButton.OnInteractEnded += delegate () { BlueButtonRelease(); };

        statusLightButton.OnInteract += delegate () { StatusLightPress(); return false; };
        statusLightButton.OnInteractEnded += delegate () { StatusLightRelease(); };

        topLeftSection.OnInteract += delegate () { SectionPress(topLeftSection); return false; };
        bottomLeftSection.OnInteract += delegate () { SectionPress(bottomLeftSection); return false; };
        bottomRightSection.OnInteract += delegate () { SectionPress(bottomRightSection); return false; };

    }

    void Start()
    {
        rule1Inputs = new List<Event>();

        rule2Input = new List<List<string>>()
        {
            new List<string>(),
            new List<string>(),
            new List<string>()
        };

        rule2CurrentIndex = 0;

        rule3Input = new List<string>();

        timeOffset = 2;

        ModuleSolved = false;

        initialBombTime = Bomb.GetTime();

        buttonPressedNum = 0;

        rule4Started = false;

        rule5Started = false;

        holdingStatus = false;


        dashOrDot = 0;

        submitting = 0;

        dotThreshold = 50;
        dashThreshold = 150;
        morseSubmitThreshold = 300;

        breakThreshold = 125;

        unicorn = Unicorn();
        rule1 = Rule1();
        rule2 = Rule2();
        rule3 = Rule3();
        rule4 = Rule4();
        rule5 = Rule5();

        string edgeworkRule = unicorn ? "Unicorn" : rule1 ? "1" : rule2 ? "2" : rule3 ? "3" : rule4 ? "4" : rule5 ? "5" : "N/A"; 

        Debug.LogFormat("[Cruel Simpleton #{0}] Edgework Rule: {1}", ModuleId, edgeworkRule);


        if (unicorn)
        {
            unicornRuleNum = 1;
            rule1Answer = FindRule1Answer();
            
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1}", ModuleId, string.Join(" ", rule1Answer.Select(e => e.ToString()).ToArray()));

            rule3Answer = FindRule3Answer();

            rule8Answer = FindRule8Answer();
            rule8Input = null;
        }

        else if (rule1)
        {
            rule1Answer = FindRule1Answer();
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1}", ModuleId, string.Join(" ", rule1Answer.Select(e => e.ToString()).ToArray()));
        }

        else if (rule2)
        {
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: -... --- -... (BOB)", ModuleId);

        }

        else if (rule3)
        {
            rule3Answer = FindRule3Answer();
            string letter = morse[rule3Answer];
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1} ({2})", ModuleId, rule3Answer, letter);
        }

        else if (rule4)
        {
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be held for 7-9 seconds", ModuleId);
        }

        else if (rule5)
        {
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be pressed {1} {2}", ModuleId, rule5Answer, rule5Answer == 1 ? "time" : "times");
        }
    }

    void Update() 
    {
        if ((rule1 || (unicorn && unicornRuleNum == 1)) && !ModuleSolved)
        {
            currentTime = (int)Bomb.GetTime();

            if (bluePressTime != currentTime && lastTime != currentTime)
            {
                lastTime = currentTime;
                rule1Inputs.Add(Event.Tick);
                CheckEvents();
            }

        }

        else if (rule5Started && !ModuleSolved)
        {
            timeOffset -= Time.deltaTime;

            if (timeOffset <= 0 && !ModuleSolved)
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("Strike! 2 seconds have passed since you pressed the button. Restarting module");
                rule5Started = false;
                buttonPressedNum = 0;
                timeOffset = 2;
            }
        }

        if (rule4Started && !ModuleSolved)
        {
            rule4StartingTime += Time.deltaTime;

            Debug.Log("Current Time Held: " + string.Format("{0:0.00}", Math.Abs(rule4EndingTime - rule4StartingTime)));
            
        }
    }

    void FixedUpdate()
    {
        if ((unicorn && unicornRuleNum == 2) || rule2)
        {
            if (holdingStatus && !ModuleSolved)
            {
                dashOrDot++;
            }

            if (dashOrDot != 0 && dashOrDot <= dotThreshold)
            {
                if (!dotSoundPlayed)
                {
                    Audio.PlaySoundAtTransform(dotSound.name, transform);
                    dotSoundPlayed = true;
                }
                Debug.Log("Dot Inputted");
            }

            if (dashOrDot > dotThreshold && dashOrDot <= dashThreshold)
            {
                if (!dashSoundPlayed)
                {
                    Audio.PlaySoundAtTransform(dashSound.name, transform);
                    dashSoundPlayed = true;
                }

                Debug.Log("Dash Inputted");
            }

            else if (dashOrDot > dashThreshold)
            {
                if (!clearSoundPlayed)
                {
                    Audio.PlaySoundAtTransform(clearMorseSound.name, transform);
                    clearSoundPlayed = true;
                }

                Debug.Log("Input Cleared");
            }

            else if (!holdingStatus && !ModuleSolved && rule2Input.Where(list => list.Any()).Any())
            {
                submitting++;
            }

            if (submitting == breakThreshold)
            {
                Audio.PlaySoundAtTransform(breakSound.name, transform);
                Debug.Log("BREAK");
                rule2CurrentIndex++;
            }

            else if (submitting == morseSubmitThreshold)
            {
                string answer = Rule2Answer();
                string modedAnswer = Rule2ModdedAnswer();

                Debug.LogFormat("[Cruel Simpleton #{0}] Submitted: {1}", ModuleId, modedAnswer);

                if (answer == "-... --- -...")
                {
                    if (unicorn && unicornRuleNum == 2)
                    {
                        //play stage clear sound
                        unicornRuleNum++;
                        submitting = 0;
                        Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                        Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                        Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1} ({2})", ModuleId, rule3Answer, Bomb.GetSerialNumber().First());
                    }

                    else
                    {
                        submitting = 0;
                        GetComponent<KMBombModule>().HandlePass();
                        ModuleSolved = true;
                        buttonText.text = "VICTORY";
                        Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                    }

                }

                else
                {
                    GetComponent<KMBombModule>().HandleStrike();

                    ClearRule2Input();
                    submitting = 0;
                    rule2CurrentIndex = 0;
                }
            }
        }

        else if ((unicorn && unicornRuleNum == 3) || rule3)
        {
            if (holdingStatus && !ModuleSolved)
            {
                dashOrDot++;
            }

            if (dashOrDot != 0 && dashOrDot <= dotThreshold)
            {
                if (!dotSoundPlayed)
                {
                    Audio.PlaySoundAtTransform(dotSound.name, transform);
                    dotSoundPlayed = true;
                }

                Debug.Log("Dot Inputted");
            }

            if (dashOrDot > dotThreshold && dashOrDot <= dashThreshold)
            {
                if (!dashSoundPlayed)
                {
                    Audio.PlaySoundAtTransform(dashSound.name, transform);
                    dashSoundPlayed = true;
                }

                Debug.Log("Dash Inputted");
            }

            else if (dashOrDot > dashThreshold)
            {
                if (!clearSoundPlayed)
                {
                    Audio.PlaySoundAtTransform(clearMorseSound.name, transform);
                    clearSoundPlayed = true;
                }

                Debug.Log("Input Cleared");
            }

            else if (!holdingStatus && !ModuleSolved && rule3Input.Count != 0)
            {
                submitting++;
            }

            if (submitting == morseSubmitThreshold)
            {
                string inputStr = string.Join("", rule3Input.ToArray());

                string letter = morse.Keys.Contains(inputStr) ? morse[inputStr] : "?";

                Debug.LogFormat("[Cruel Simpleton #{0}] Submitted: {1} ({2})", ModuleId, inputStr, letter);

                if (Rule3Correct())
                {
                    if (unicorn && unicornRuleNum == 3)
                    {
                        unicornRuleNum++;
                        submitting = 0;
                        Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                        Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                        Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be held for 8 seconds", ModuleId);
                    }

                    else
                    {
                        ModuleSolved = true;
                        submitting = 0;
                        GetComponent<KMBombModule>().HandlePass();
                        buttonText.text = "VICTORY";
                        Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                    }
                }

                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    rule3Input.Clear();
                    submitting = 0;
                }
            }
        }
    }

    #region Rules

    private bool Unicorn()
    {
        //if there are 2 batteries in 2 holders, 2 indicators, a DVI, RJ-45, PS2, and RCA ports on the same port plate, and the serial number contains a "U"

        int holderNum = Bomb.GetBatteryHolderCount();
        int batteryNum = Bomb.GetBatteryCount();
        int indicatorNum = Bomb.GetIndicators().Count();
        string serialNumber = Bomb.GetSerialNumber();

        bool fullPlate = Bomb.GetPortPlates().Where(plate => plate.Contains("DVI") && plate.Contains("PS2") && plate.Contains("RJ45") && plate.Contains("StereoRCA")).Any();


        return holderNum == 2 && batteryNum == 2 && indicatorNum == 2 && fullPlate && serialNumber.ToUpper().Contains('U');
    }

    private bool Rule1()
    {
        //If the serial number contains four numbers and two letters

        if (!(Bomb.GetSerialNumberNumbers().Count() == 4 && Bomb.GetSerialNumberLetters().Count() == 2))
        {
            return false;
        }

        return !unicorn;
    }

    private bool Rule2()
    {
        //if there is a lit BOB indicator
        if (!(Bomb.IsIndicatorOn(Indicator.BOB)))
        {
            return false;
        }

        return !unicorn && !rule1;
    }

    private bool Rule3()
    {
        //if there is a Parallel port and Serial port on the same port plate
        if (!Bomb.GetPortPlates().Where(plate => plate.Contains("Parallel") && plate.Contains("Serial")).Any())
        {
            return false;
        }

        return !unicorn && !rule1 && !rule2;
    }

    private bool Rule4()
    {
        //if there is 4 batteries in 2 holders
        if (!(Bomb.GetBatteryCount() == 4 && Bomb.GetBatteryHolderCount() == 2))
        {
            return false;
        }

        return !unicorn && !rule1 && !rule2 && !rule3;
    }

    private bool Rule5()
    {
        //there is a simpleton
        if (!Bomb.GetModuleNames().Where(name => name.ToUpper() == "THE SIMPLETON").Any())
        {
            return false;
        }

        return !unicorn && !rule1 && !rule2 && !rule3 && !rule4;
    }

    private bool Rule6()
    {
        //more than half the time is on the bomb has passed
        if (!(Bomb.GetTime() < initialBombTime / 2))
        {
            return false;
        }

        return !unicorn && !rule1 && !rule2 && !rule3 && !rule4 && !rule5;
    }

    private bool Rule7()
    {
        if (!(Bomb.GetStrikes() > 0))
        {
            return false;
        }

        return !unicorn && !rule1 && !rule2 && !rule3 && !rule4 && !rule5 && !Rule6();
    }

    private bool Rule8()
    {
        //total mod count is prime 
        if (!ModCountPrime())
        {
            return false;
        }

        return !unicorn && !rule1 && !rule2 && !rule3 && !rule4 && !rule5 && !Rule6() && !Rule7();
    }

    private bool Rule9()
    {
        return !unicorn && !rule1 && !rule2 && !rule3 && !rule4 && !rule5 && !Rule6() && !Rule7() && !Rule8();
    }

    #endregion

    #region Events
    private void BlueButtonPress()
    {
        blueButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        bool unicorn1Active = unicorn && unicornRuleNum == 1;
        bool unicorn2Active = unicorn && unicornRuleNum == 2;
        bool unicorn3Active = unicorn && unicornRuleNum == 3;
        bool unicorn4Active = unicorn && unicornRuleNum == 4;
        bool unicorn5Active = unicorn && unicornRuleNum == 5;
        bool unicorn6Active = unicorn && unicornRuleNum == 6;
        bool unicorn8Active = unicorn && unicornRuleNum == 8;
        bool unicorn9Active = unicorn && unicornRuleNum == 9;


        bool rule1Active = rule1;
        bool rule2Active = rule2;
        bool rule3Active = rule3;
        bool rule4Active = rule4;
        bool rule5Active = rule5;
        bool rule6Active = Rule6();
        bool rule8Active = Rule8();
        bool rule9Active = Rule9();

        if (ModuleSolved)
        {
            return;
        }

        if (rule1Active || unicorn1Active)
        {
            if (!mouseDown)
            {
                rule1Inputs.Add(Event.MouseDown);
                CheckEvents();
            }

            bluePressTime = (int)Bomb.GetTime();
            mouseDown = true;
            return;
        }

        if (rule4Active || unicorn4Active)
        {
            rule4StartingTime = Time.time;
            rule4EndingTime = rule4StartingTime;
            rule4Started = true;
            return;
        }



        if (rule5Active || unicorn5Active)
        {
            rule5Started = true;
            timeOffset = 2f;
            buttonPressedNum++;

            Debug.Log("Button has been pressed " + buttonPressedNum + " times");


            if (buttonPressedNum == rule5Answer)
            {
                rule5Started = false;

                if (unicorn && unicornRuleNum == 5)
                {
                    //play clear stage sound
                    unicornRuleNum++;
                    Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                    Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                }

                else
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    buttonText.text = "VICTORY";
                    Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                }

            }
            return;
        }


        if (rule2Active || unicorn2Active || 
            rule3Active || unicorn3Active || 
            rule8Active || unicorn8Active)
        {
            return;
        }


        if (!rule6Active && !unicorn6Active && !rule9Active && !unicorn9Active && !strikeAlreadyGiven)
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed the button when rule 6 didn't apply", ModuleId);
            strikeAlreadyGiven = false;
            return;
        }

        if (rule6Active || unicorn6Active)
        {
            int minutes = (int)Bomb.GetTime() / 60;
            int seconds = (int)Bomb.GetTime() % 60;
            string time = minutes + ":" + seconds;
            

            Debug.LogFormat("[Cruel Simpleton #{0}] Button was pressed at {1} (Rule 6)", ModuleId, time);

            if (seconds % 10 != 0)
            {
                GetComponent<KMBombModule>().HandleStrike();
            }

            else
            {
                if (unicorn && unicornRuleNum == 6)
                {
                    //play stage clear sound
                    unicornRuleNum++;
                    Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                    Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                }

                else
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    buttonText.text = "VICTORY";
                    Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                }
            }

            return;

        }


        if (rule9Active || unicorn9Active)
        {
            Debug.LogFormat("[Cruel Simpleton #{0}] Pressed the button (Rule 9)", ModuleId);
            GetComponent<KMBombModule>().HandlePass();
            ModuleSolved = true;
            buttonText.text = "VICTORY";
            Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
        }
    }

    private void BlueButtonRelease()
    {
        if (ModuleSolved)
        {
            return;
        }

        if (mouseDown && rule1Inputs.Count(e => e == Event.MouseDown) > rule1Inputs.Count(e => e == Event.MouseUp))
        {
            rule1Inputs.Add(Event.MouseUp);
            bluePressTime = (int)Bomb.GetTime();
            CheckEvents();
        }
        mouseDown = false;

        if (!rule4 && !(unicorn && unicornRuleNum == 4))
        {
            return;
        }

        rule4Started = false;

        float deltaTime = Math.Abs(rule4EndingTime - rule4StartingTime);

        string time = string.Format("{0:0.#}", deltaTime);

        Debug.LogFormat("[Cruel Simpleton #{0}] Held button for {1} seconds", ModuleId, time);

        float minValue = 7f;
        float maxValue = 9f;

        if (minValue <= deltaTime && deltaTime <= maxValue)
        {
            if (unicorn && unicornRuleNum == 4)
            {
                //play clear stage sound
                unicornRuleNum++;
                Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be pressed {1} {2}", ModuleId, rule5Answer, rule5Answer == 1 ? "time" : "times");
            }

            else
            {
                GetComponent<KMBombModule>().HandlePass();
                ModuleSolved = true;
                buttonText.text = "VICTORY";
                Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
            }

        }

        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
    }

    private void SectionPress(KMSelectable section)
    {
        section.AddInteractionPunch(0.1f);

        string sectionName = SectionToName(section);
        int sectionNum = SectionToInt(section);

        if (sectionNum == 4)
        { 
            Debug.Log("Pressed section button");
        }

        bool unicorn1Active = unicorn && unicornRuleNum == 1;
        bool unicorn2Active = unicorn && unicornRuleNum == 2;
        bool unicorn3Active = unicorn && unicornRuleNum == 3;
        bool unicorn4Active = unicorn && unicornRuleNum == 4;
        bool unicorn5Active = unicorn && unicornRuleNum == 5;
        bool unicorn6Active = unicorn && unicornRuleNum == 6;
        bool unicorn7Active = unicorn && unicornRuleNum == 7;
        bool unicorn8Active = unicorn && unicornRuleNum == 8;
        bool unicorn9Active = unicorn && unicornRuleNum == 9;


        bool rule1Active = rule1;
        bool rule2Active = rule2;
        bool rule3Active = rule3;
        bool rule4Active = rule4;
        bool rule5Active = rule5;
        bool rule6Active = Rule6();
        bool rule7Active = Rule7();
        bool rule8Active = Rule8();
        bool rule9Active = Rule9();

        if (ModuleSolved)
        {
            return;
        }


        if (rule7Active || unicorn7Active)
        {
            rule7Answer = FindRule7Answer();

            Debug.LogFormat("[Cruel Simpleton #{0}] Pressed section {1}. Expected section {2} (Rule 7)", ModuleId, sectionNum, rule7Answer);

            if (sectionNum == rule7Answer)
            {
                if (unicorn && unicornRuleNum == 7)
                {
                    //play stage clear sound
                    unicornRuleNum++;
                    Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                    Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);

                }

                else
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    buttonText.text = "VICTORY";
                    Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                }

            }

            else
            {
                strikeAlreadyGiven = true;
                GetComponent<KMBombModule>().HandleStrike();
            }

            return;

        }


        if (rule8Active || unicorn8Active)
        {
            if (rule8Input == null)
            {
                rule8Input = new List<int>();
            }

            if (rule8Answer == null)
            {
                rule8Answer = FindRule8Answer();
            }

            rule8Input.Add(sectionNum);

            int index = rule8Input.Count - 1;

            if (rule8Input.Last() != rule8Answer[index])
            {
                GetComponent<KMBombModule>().HandleStrike();

                string input = "";
                string answer = "";
                for (int i = 0; i <= index; i++)
                {
                    input += rule8Input[i] + " ";
                    answer += rule8Answer[i] + " ";
                }

                input = input.Trim();
                answer = answer.Trim();

                Debug.LogFormat("[Cruel Simpleton #{0}] Submitted: {1} instead of {2} (Rule 8)", ModuleId, input, answer);
                rule8Input.Clear();
                return;
            }

            if (rule8Input.Count == rule8Answer.Count)
            {
                if (unicorn && unicornRuleNum == 8)
                {
                    //play stage clear sound
                    unicornRuleNum++;
                    Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                    Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);

                }

                else
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    buttonText.text = "VICTORY";
                    Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                }

                return;
            }
        }

        if (rule2Active || unicorn2Active ||
            rule3Active || unicorn3Active)
        {
            if (sectionNum == 4)
            {
                Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed the button instead of the status light", ModuleId);
            }

            else
            {
                Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed {1} instead of the status light", ModuleId, sectionName);
            }

            GetComponent<KMBombModule>().HandleStrike();
            return;
        }

        if (rule1Active || unicorn1Active ||
            rule4Active || unicorn4Active ||
            rule5Active || unicorn5Active ||
            rule6Active || unicorn6Active || 
            rule9Active || unicorn9Active)
        {
            if (sectionNum != 4)
            {
                if (rule1Active || unicorn1Active || rule4Active || unicorn4Active || rule5Active || unicorn5Active)
                {
                    Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed {1} instead of the button", ModuleId, sectionName);
                }

                else
                {
                    string rule = rule6Active || unicorn6Active ? "(Rule 6)" : "(Rule 9)";
                    Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed {1} instead of the button {2}", ModuleId, sectionName, rule);
                }

                GetComponent<KMBombModule>().HandleStrike();
                return;
            }
        }
    }

    private void StatusLightPress()
    {
        statusLightButton.AddInteractionPunch(0.1f);

        bool rule1Active = rule1;
        bool rule2Active = rule2;
        bool rule3Active = rule3;
        bool rule4Active = rule4;
        bool rule5Active = rule5;
        bool rule6Active = Rule6();
        bool rule7Active = Rule7();
        bool rule8Active = Rule8();
        bool rule9Active = Rule9();

        bool unicorn1Active = unicorn && unicornRuleNum == 1;
        bool unicorn2Active = unicorn && unicornRuleNum == 2;
        bool unicorn3Active = unicorn && unicornRuleNum == 3;
        bool unicorn4Active = unicorn && unicornRuleNum == 4;
        bool unicorn5Active = unicorn && unicornRuleNum == 5;
        bool unicorn6Active = unicorn && unicornRuleNum == 6;
        bool unicorn7Active = unicorn && unicornRuleNum == 7;
        bool unicorn8Active = unicorn && unicornRuleNum == 8;
        bool unicorn9Active = unicorn && unicornRuleNum == 9;

        if ((rule2Active || unicorn2Active) && rule2CurrentIndex < 3)
        {
            submitting = 0;
            holdingStatus = true;
        }

        if (rule3Active || unicorn3Active)
        {
            submitting = 0;
            holdingStatus = true;
        }

        if (rule7Active || unicorn7Active ||
            rule8Active || unicorn8Active)
        {
            string rule = rule7Active || unicorn7Active ? "(Rule 7)" : "(Rule 8)";
            Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed stataus light instead of one of the sections {1}", ModuleId, rule);
            GetComponent<KMBombModule>().HandleStrike();
            return;
        }


        if (rule1Active || unicorn1Active ||
            rule4Active || unicorn4Active ||
            rule5Active || unicorn5Active || 
            rule6Active || unicorn6Active || 
            rule9Active || unicorn9Active)
        {
            string rule = rule6Active || unicorn6Active ? "(Rule 6)" : rule9Active || unicorn9Active ? "(Rule 9)" : "";

            Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed status light instead of the button {1}", ModuleId, rule);
            GetComponent<KMBombModule>().HandleStrike();
            return;
        }
    }

    private void StatusLightRelease()
    {
        bool unicorn2Active = unicorn && unicornRuleNum == 2;
        bool unicorn3Active = unicorn && unicornRuleNum == 3;

        if ((rule2 || unicorn2Active) && holdingStatus)
        {
            holdingStatus = false;
            dotSoundPlayed = false;
            dashSoundPlayed = false;
            clearSoundPlayed = false;

            if (dashOrDot <= dotThreshold)
            {
                rule2Input[rule2CurrentIndex].Add(".");
            }

            else if (dashOrDot > dotThreshold && dashOrDot <= dashThreshold)
            {
                rule2Input[rule2CurrentIndex].Add("-");
            }

            else if (dashOrDot > dashThreshold)
            {
                rule2CurrentIndex = 0;

                ClearRule2Input();
            }

            dashOrDot = 0;


            Debug.Log("Input is now: " + Rule2Answer());
        }

        else if ((rule3 || unicorn3Active) && holdingStatus)
        {
            holdingStatus = false;
            dotSoundPlayed = false;
            dashSoundPlayed = false;
            clearSoundPlayed = false;
            holdingStatus = false;

            if (dashOrDot <= dotThreshold)
            {
                rule3Input.Add(".");
            }

            else if (dashOrDot > dotThreshold && dashOrDot <= dashThreshold)
            {
                rule3Input.Add("-");
            }

            else if (dashOrDot > dashThreshold)
            {
                rule3Input.Clear();
            }

            dashOrDot = 0;


            Debug.Log("Input is now: " + string.Join("", rule3Input.ToArray()));
        }
    }


    #endregion

    #region Find Answers

    private List<Event> FindRule1Answer()
    {
        char letter = Bomb.GetSerialNumberLetters().Last();

        int num = (letter - 64) % 5;

        switch (num)
        {
            case 0:
                return new List<Event>() { Event.Tick, Event.MouseDown, Event.Tick, Event.MouseUp, Event.Tick };

            case 1:
                return new List<Event>() { Event.Tick, Event.MouseDown, Event.MouseUp, Event.Tick, Event.MouseDown, Event.MouseUp, Event.Tick };

            case 2:
                return new List<Event>() { Event.Tick, Event.MouseDown, Event.MouseUp, Event.Tick, Event.MouseDown, Event.Tick, Event.MouseUp, Event.Tick };

            case 3:
                return new List<Event>() { Event.Tick, Event.MouseDown, Event.Tick, Event.MouseUp, Event.MouseDown, Event.Tick, Event.MouseUp, Event.Tick };

            default:
                return new List<Event>() { Event.Tick, Event.MouseDown, Event.Tick, Event.Tick, Event.MouseUp, Event.Tick };

        }
    }

    private string FindRule3Answer()
    {
        string firstChar = "" + Bomb.GetSerialNumber().First();

        KeyValuePair<string, string> pair = morse.First(p => morse[p.Key] == firstChar);

        return pair.Key;
    }


    private int FindRule7Answer()
    {
        int strikes = Bomb.GetStrikes();

        if (strikes > 0 && strikes <= 2)
        {
            return strikes;
        }

        return 3;
    }

    private List<int> FindRule8Answer()
    {
        int modNum = Bomb.GetModuleNames().Count();

        Debug.Log("Mod count: " + modNum);

        char[] charArr = modNum.ToString().ToCharArray();


        List<int> answer = new List<int>();

        foreach (char str in charArr)
        {
            int num = int.Parse("" + str) % 5;

            if (num == 0)
            {
                num = 4;
            }

            answer.Add(num);
        }

        return answer;
    }

    #endregion

    #region Helper Methods

    private int SectionToInt(KMSelectable section)
    {
        if (section == topLeftSection)
        {
            return 1;
        }

        if (section == bottomLeftSection)
        {
            return 2;
        }

        if (section == bottomRightSection)
        {
            return 3;
        }

        return 4;
    }

    private string SectionToName(KMSelectable section)
    {
        if (section == topLeftSection)
        {
            return "top left";
        }

        if (section == bottomLeftSection)
        {
            return "bottom left";
        }

        if (section == bottomRightSection)
        {
            return "bottom right";
        }

        return "button";
    }

    private bool ModCountPrime()
    {
        //total mod count is prime 

        int moduleNum = Bomb.GetModuleNames().Count();

        if (moduleNum == 1) return false;
        if (moduleNum == 2) return true;

        var limit = Math.Ceiling(Math.Sqrt(moduleNum)); //hoisting the loop limit

        for (int i = 2; i <= limit; ++i)
            if (moduleNum % i == 0)
                return false;

        return true;
    }

    //Tells if the rule 3 are correct
    private bool Rule1Correct()
    {
        if (rule1Answer.Count != rule1Inputs.Count)
        {
            Debug.LogError("Sizes are different");
            return false;
        }

        for (int i = 0; i < rule1Answer.Count; i++)
        {
            if (rule1Answer[i] != rule1Inputs[i])
            {
                Debug.LogError("Index " + i + " are different. Answer (" + rule1Answer[i] + "). Inputted (" + rule1Inputs[i] + ")");

                return false;
            }
        }

        return true;
    }

    private bool Rule3Correct()
    {
        if (rule3Answer.Length != rule3Input.Count)
        {
            Debug.LogError("Lengths are not equal");
            return false;
        }

        for (int i = 0; i < rule3Answer.Length; i++)
        {
            if (rule3Answer[i].ToString() != rule3Input[i])
            {
                return false;
            }
        }

        return true;
    }

    private string Rule2Answer()
    {
        return string.Join("", rule2Input[0].ToArray()) + " " + string.Join("", rule2Input[1].ToArray()) + " " + string.Join("", rule2Input[2].ToArray());
    }

    private string Rule2ModdedAnswer()
    {
        string str1 = string.Join("", rule2Input[0].ToArray());
        string str2 = string.Join("", rule2Input[1].ToArray());
        string str3 = string.Join("", rule2Input[2].ToArray());

        string letter1 = morse.Keys.Contains(str1) ? morse[str1] : "?";
        string letter2 = morse.Keys.Contains(str2) ? morse[str2] : "?";
        string letter3 = morse.Keys.Contains(str3) ? morse[str3] : "?";

        return str1 + " " + str2 + " " + str3 + " (" + letter1 + letter2 + letter3 + ")";
    }

    private void ClearRule2Input()
    {
        rule2Input = new List<List<string>>()
        {
            new List<string>(),
            new List<string>(),
            new List<string>()
        };
    }

    private bool ThreeTickGoneBy()
    {
        if (rule1Inputs.Count < 3)
        {
            return false;
        }

        return rule1Inputs[rule1Inputs.Count - 1] == Event.Tick && rule1Inputs[rule1Inputs.Count - 2] == Event.Tick && rule1Inputs[rule1Inputs.Count - 3] == Event.Tick;

    }

    private void CheckEvents()
    {
        if (ModuleSolved)
        {
            return;
        }

        while (rule1Inputs.Count >= 2 && rule1Inputs[0] == Event.Tick && (rule1Inputs[1] == Event.Tick || rule1Inputs[1] == Event.MouseUp))
            rule1Inputs.RemoveAt(1);

        //if more than 2 tick have gone by, check to see if the answer is correct
        //if the number of events equal the number of events in the answer, check the answer

        if (ThreeTickGoneBy() || rule1Inputs.Count == rule1Answer.Count)
        {
            Debug.LogFormat("[Cruel Simpleton #{0}] Submitted: {1}", ModuleId, string.Join(", ", rule1Inputs.Select(e => e.ToString()).ToArray()));

            if (Rule1Correct())
            {
                if (unicorn && unicornRuleNum == 1)
                {
                    //play stage clear sound
                    unicornRuleNum++;
                    Audio.PlaySoundAtTransform(stageClearSound.name, transform);

                    Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1} ", ModuleId, unicornRuleNum);
                    Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: -... --- -... (BOB)", ModuleId);
                }

                else
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    buttonText.text = "VICTORY";
                    Audio.PlaySoundAtTransform(moduleSolveSound.name, transform);
                }

                return;
            }

            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                rule1Inputs.Clear();
                return;
            }
        }
    }
    #endregion







#pragma warning disable 414
    private readonly string TwitchHelpMessage = "For black hole, use \"!{0} black\" followed by \"hold #\" to hold the button # number of tick down, \"wait #\" to wait # ticks, and \"press\" to tap and immediately release. Commands be chained with a space in between. For morse code, \"use !{0}\" followed by \".\" and \"-\". Ex .- = A. Chain codes with a space between. For section presses, use \"!{0} press\" followed by the number of the section. Use \"!{0} press 1 at 30\" to press the section at 30 seconds. Sections can be chained with no spaces between. Times can be chained with spaces between. Use \"!{0} hold #\" to hold the button for # seconds.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) 
   {
        string[] commandArr = Command.ToUpper().Trim().Split(' ');
        yield return null;


        List<int> times = new List<int>();

        //pressing section
        if (commandArr[0] == "PRESS" && ValidNum(commandArr[1]))
        {
            //pressing section at a certain time
            if (commandArr.Length > 3 && commandArr[2] == "AT")
            {
                int num;

                int.TryParse(commandArr[1], out num);

                if (!Between1And4(commandArr[1]))
                {
                    yield return string.Format("sendtochaterror {0} is not a valid section", num);
                    yield break;
                }

                for (int i = 3; i < commandArr.Length; i++)
                {
                    int time;

                    if (int.TryParse(commandArr[i], out time) && time >= 0 && time <= 59)
                    {
                        times.Add(time);
                    }

                    else
                    {
                        yield return string.Format("sendtochaterror {0} is not a valid time", commandArr[i]);
                        yield break;
                    }
                }

                while (!times.Contains((int)Bomb.GetTime() % 60))
                {
                    yield return "trycancel input has been cancelled";
                }

                switch (num)
                {
                    case 1: topLeftSection.OnInteract(); break;
                    case 2: bottomLeftSection.OnInteract(); break;
                    case 3: bottomRightSection.OnInteract(); break;
                    case 4: blueButton.OnInteract(); break;

                    default:
                        yield return string.Format("sendtochaterror {0} is not a valid section", num);
                        yield break;
                }
            }

            //press multiple sections
            else
            {
                char invalidChar = '\0';
                try
                {
                    invalidChar = commandArr[1].Where(x => int.Parse("" + x) < 1 || int.Parse("" + x) > 4).First();
                }

                catch
                { }

                if (invalidChar != '\0')
                {
                    yield return string.Format("sendtochaterror {0} is not a valid section", invalidChar);
                    yield break;
                }

                for (int i = 0; i < commandArr[1].Length; i++)
                {
                    switch (int.Parse("" + commandArr[1][i]))
                    {
                        case 1: topLeftSection.OnInteract(); break;
                        case 2: bottomLeftSection.OnInteract(); break;
                        case 3: bottomRightSection.OnInteract(); break;
                        case 4: blueButton.OnInteract(); break;
                    }
                }
            }
        }

        //black hole
        else if (commandArr[0] == "BLACK")
        {
            //BLACK MUST BE FOLLOWED BY A HOLD OR PRESS
            if (commandArr.Length == 1 || (commandArr[1] != "HOLD" && commandArr[1] != "PRESS"))
            {
                yield return string.Format("sendtochaterror \"Black\" must be followed by \"Hold\" or \"Press\"");
                yield break;
            }

            int num;
            //check to see if there is number folloewed by every hold or wait command
            for (int i = 1; i < commandArr.Length; i++)
            {
                if ((commandArr[i] == "HOLD" || commandArr[i] == "WAIT") && (i + 1 == commandArr.Length || !int.TryParse(commandArr[i + 1], out num)))
                {
                    yield return string.Format("sendtochaterror Every hold and wait command must be followed by a number");
                    yield break;
                }
            }

            //parsing input
            List<Event> events = new List<Event>();

            for (int i = 0; i < commandArr.Length; i++)
            {
                string s = commandArr[i];

                switch (s)
                {
                    case "PRESS":
                        events.Add(Event.MouseDown);
                        events.Add(Event.MouseUp);
                        break;

                    case "HOLD":
                        events.Add(Event.MouseDown);
                        for (int j = 0; j < int.Parse(commandArr[i + 1]); j++)
                        {
                            events.Add(Event.Tick);
                        }
                        events.Add(Event.MouseUp);
                        break;

                    case "WAIT":
                        for (int j = 0; j < int.Parse(commandArr[i + 1]); j++)
                        {
                            events.Add(Event.Tick);
                        }
                        break;
                }
            }

            //doing input
            foreach (Event e in events)
            {
                switch (e)
                {
                    case Event.MouseUp:
                        blueButton.OnInteractEnded();
                        break;

                    case Event.MouseDown:
                        blueButton.OnInteract();
                        break;

                    case Event.Tick:
                        var time = (int)Bomb.GetTime();
                        yield return new WaitUntil(() => (int)Bomb.GetTime() != time);
                        break;
                }
            }
        }

        //hold blue button
        else if (commandArr[0] == "HOLD")
        {
            //check to see if the player added time at the end

            int sec;

            if (commandArr.Length == 1 || !int.TryParse(commandArr[1], out sec))
            {
                yield return string.Format("sendtochaterror Missing how long to hold the button");
                yield break;
            }

            blueButton.OnInteract();
            
            yield return new WaitUntil(() => Math.Abs(rule4EndingTime - rule4StartingTime) >= sec);

            blueButton.OnInteractEnded();
        }

        //morse code
        else if (commandArr[0][0] == '.' || commandArr[0][0] == '-')
        {
            //verify each command is just . and -

            foreach (string c in commandArr)
            {
                if (c.Any(x => x != '.' && x != '-'))
                {
                    yield return string.Format("sendtochaterror Morse code must be submitted with just . and -");
                    yield break;
                }
            }

            //morse was not expected
            if (!(unicorn && unicornRuleNum == 3) || rule3Answer == null)
            {
                statusLightButton.OnInteract();
                statusLightButton.OnInteractEnded();
                yield break;
            }

            foreach (string command in commandArr)
            {
                foreach (char c in command)
                {
                    if (c == '.')
                    {
                        statusLightButton.OnInteract();
                        statusLightButton.OnInteractEnded();
                    }

                    else
                    {
                        statusLightButton.OnInteract();

                        yield return new WaitUntil(() => dashOrDot > dotThreshold);

                        statusLightButton.OnInteractEnded();

                        Debug.Log("Interact Ended");
                    }
                }

                //wait for break
                yield return new WaitUntil(() => submitting >= breakThreshold);

            }

            //wait for more submission
            yield return new WaitUntil(() => submitting == 0);



        }

        else
        {
            yield return string.Format("sendtochaterror Invalid command");
            yield break;
        }
   }

   IEnumerator TwitchHandleForcedSolve () {

        if (unicorn)
        {
            yield return ProcessTwitchCommand(GetRule1TPAnswer());
            yield return ProcessTwitchCommand("-... --- -...");
            yield return ProcessTwitchCommand(rule3Answer);
            yield return ProcessTwitchCommand("hold 7");
            yield return ProcessTwitchCommand("press 4444444444");
            yield return ProcessTwitchCommand("press 4 at 0 10 20 30 40 50");


            yield return ProcessTwitchCommand("press " + FindRule7Answer());

            rule8Answer = FindRule8Answer();

            yield return ProcessTwitchCommand(GetRule8TPAnswer());
            yield return ProcessTwitchCommand("press 4");
        }

        else if (rule1)
        {
            yield return ProcessTwitchCommand(GetRule1TPAnswer());
        }

        else if (rule2)
        {
            yield return ProcessTwitchCommand("-... --- -...");
        }

        else if (rule3)
        {
            yield return ProcessTwitchCommand(rule3Answer);
        }

        else if (rule4)
        {
            yield return ProcessTwitchCommand("hold 7");
        }

        else if (rule5)
        {
            yield return ProcessTwitchCommand("press 4444444444");
        }

        else if (Rule6())
        {
            yield return ProcessTwitchCommand("press 4 at 0 10 20 30 40 50");
        }

        else if (Rule7())
        {
            yield return ProcessTwitchCommand("press " + (rule7Answer + 1));
        }

        else if (Rule8())
        {
            rule8Answer = FindRule8Answer();
            yield return ProcessTwitchCommand(GetRule8TPAnswer());
        }

        else
        { 
            yield return ProcessTwitchCommand("press 4");
        }

        while (!ModuleSolved)
        {
            yield return true;
        }
    }

    private string GetRule1TPAnswer()
    {
        string command = "black ";

        int digit = (Bomb.GetSerialNumberLetters().Last() - 64) % 5;

        switch (digit)
        {
            case 0:
                command += "hold 1";
                break;

            case 1:
                command += "press wait 1 press";
                break;

            case 2:
                command += "press wait 1 hold 1";
                break;

            case 3:
                command += "hold 1 hold 1";
                break;

            default:
                command += "hold 2";
                break;

        }

        command += " wait 1";

        return command;
    }

    private string GetRule8TPAnswer()
    {
        string[] arr = rule8Answer.Select(x => x.ToString()).ToArray();

        return "press " + string.Join("", arr);
    }

    private bool ValidNum(string num)
    {
        return !num.Select(c => Char.IsDigit(c)).Contains(false);
    }

    private bool Between1And4(string num)
    { 
        return !num.Select(c => int.Parse("" + c) < 1 || int.Parse("" + c) > 4).Contains(true);
    }
}
