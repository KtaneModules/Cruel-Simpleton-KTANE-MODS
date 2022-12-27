using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

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

    public AudioSource audioSource;

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
    private int rule3SubmitThreshold;
    private int rule2SubmitThreshold;




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
    private bool rule8;


    private List<Event> rule1Inputs;

    private List<Event> rule1Answer;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;


    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        blueButton.OnInteract += delegate () { SectionPress(blueButton); return false; };

        blueButton.OnInteract += delegate () { BlueButton(); return false; };

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
        rule3SubmitThreshold = 125;

        breakThreshold = 125;
        rule2SubmitThreshold = 300;

        unicorn = Unicorn();
        rule1 = Rule1();
        rule2 = Rule2();
        rule3 = Rule3();
        rule4 = Rule4();
        rule5 = Rule5();
        rule8 = Rule8();

        string edgeworkRule = unicorn ? "Unicorn" : rule1 ? "1" : rule2 ? "2" : rule3 ? "3" : rule4 ? "4" : rule5 ? "5" : rule8 ? "8" : "N/A"; 

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
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: -... --- -...", ModuleId);

        }

        else if (rule3)
        {
            rule3Answer = FindRule3Answer();
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1}", ModuleId, rule3Answer);
        }

        else if (rule4)
        {
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be held for 8 seconds", ModuleId);
        }

        else if (rule5)
        { 
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be pressed 69 times", ModuleId);
        }

        else if (rule8)
        {
            rule8Answer = FindRule8Answer();
            rule8Input = null;
            Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1}", ModuleId, string.Join(" ", rule8Answer.Select(x => x.ToString()).ToArray()));

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
                Debug.Log("Strike! 2 seconds have passed since you pressed the button. Restarting module");
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
                    audioSource.clip = dotSound;
                    audioSource.Play();
                    dotSoundPlayed = true;
                }
                Debug.Log("Dot Inputted");
            }

            if (dashOrDot > dotThreshold && dashOrDot <= dashThreshold)
            {
                if (!dashSoundPlayed)
                {
                    audioSource.clip = dashSound;
                    audioSource.Play();
                    dashSoundPlayed = true;
                }

                Debug.Log("Dash Inputted");
            }

            else if (dashOrDot > dashThreshold)
            {
                if (!clearSoundPlayed)
                {
                    audioSource.clip = clearMorseSound;
                    audioSource.Play();
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
                audioSource.clip = breakSound;
                audioSource.Play();
                Debug.Log("BREAK");
                rule2CurrentIndex++;
            }

            else if (submitting == rule2SubmitThreshold)
            {
                string answer = Rule2Answer();

                Debug.LogFormat("[Cruel Simpleton #{0}] Submitted {1}", ModuleId, answer);

                if (answer == "-... --- -...")
                {
                    if (unicorn && unicornRuleNum == 2)
                    {
                        //play stage clear sound
                        unicornRuleNum++;
                        audioSource.clip = stageClearSound;
                        audioSource.Play();
                        Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                        Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: {1}", ModuleId, rule3Answer);

                    }

                    else
                    {
                        submitting = 0;
                        GetComponent<KMBombModule>().HandlePass();
                        ModuleSolved = true;
                        buttonText.text = "VICTORY";
                        audioSource.clip = moduleSolveSound;
                        audioSource.Play();
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
                    audioSource.clip = dotSound;
                    audioSource.Play();
                    dotSoundPlayed = true;
                }

                Debug.Log("Dot Inputted");
            }

            if (dashOrDot > dotThreshold && dashOrDot <= dashThreshold)
            {
                if (!dashSoundPlayed)
                {
                    audioSource.clip = dashSound;
                    audioSource.Play();
                    dashSoundPlayed = true;
                }

                Debug.Log("Dash Inputted");
            }

            else if (dashOrDot > dashThreshold)
            {
                if (!clearSoundPlayed)
                {
                    audioSource.clip = clearMorseSound;
                    audioSource.Play();
                    clearSoundPlayed = true;
                }

                Debug.Log("Input Cleared");
            }

            else if (!holdingStatus && !ModuleSolved && rule3Input.Count != 0)
            {
                submitting++;
            }

            if (submitting == rule3SubmitThreshold)
            {
                Debug.LogFormat("[Cruel Simpleton #{0}] Submitted {1}", ModuleId, string.Join("",rule3Input.ToArray()));

                if (Rule3Correct())
                {
                    if (unicorn && unicornRuleNum == 3)
                    {
                        unicornRuleNum++;
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
        return !unicorn && !rule1 && !rule2 && !rule3 && !rule4 && !rule5 && !Rule6() && !Rule7() && !rule8;
    }

    #endregion

    #region Events
    private void BlueButton()
    {
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
        bool rule8Active = rule8;
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


            if (buttonPressedNum == 69)
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
            rule7Active || unicorn7Active ||
            rule8Active || unicorn8Active)
        {
            return;
        }


        if (!rule6Active && !unicorn6Active && !rule9Active && !unicorn9Active)
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Cruel Simpleton #{0}] Strike! Pressed the button when rule 6 didn't apply", ModuleId);
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

        blueButton.AddInteractionPunch(0.1f);

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

        float minValue = 7.5f;
        float maxValue = 8.5f;

        if (minValue <= deltaTime && deltaTime <= maxValue)
        {
            if (unicorn && unicornRuleNum == 4)
            {
                //play clear stage sound
                unicornRuleNum++;
                Audio.PlaySoundAtTransform(stageClearSound.name, transform);
                Debug.LogFormat("[Cruel Simpleton #{0}] Stage cleared. Now on stage {1}", ModuleId, unicornRuleNum);
                Debug.LogFormat("[Cruel Simpleton #{0}] Expecting button to be pressed 69 times", ModuleId);
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
        bool rule8Active = rule8;
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

                Debug.LogFormat("[Cruel Simpleton #{0}] Submitted {1} instead of {2}", ModuleId, input, answer);
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
        bool rule8Active = rule8;
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
        switch (Bomb.GetSerialNumberLetters().First())
        {
            case 'A':
                return ".-";

            case 'B':
                return "-...";

            case 'C':
                return "-.-.";

            case 'D':
                return "-..";

            case 'E':
                return ".";

            case 'F':
                return "..-.";

            case 'G':
                return "--.";

            case 'H':
                return "....";

            case 'I':
                return "..";

            case 'J':
                return ".---";

            case 'K':
                return "-.-";

            case 'L':
                return ".-..";

            case 'M':
                return "--";

            case 'N':
                return "-.";

            case 'O':
                return "---";

            case 'P':
                return ".--.";

            case 'Q':
                return "--.-";

            case 'R':
                return ".-.";

            case 'S':
                return "...";

            case 'T':
                return "-";

            case 'U':
                return "..-";

            case 'V':
                return "...-";

            case 'W':
                return ".--";

            case 'X':
                return "-..-";

            case 'Y':
                return "-.--";

            default:
                return "--..";
        }
    }


    private int FindRule7Answer()
    {
        int strikes = Bomb.GetStrikes();

        if (strikes == 0)
        {
            return 4;
        }

        while (strikes > 4)
        {
            strikes -= 4;
        }

        return strikes;
    }

    private List<int> FindRule8Answer()
    {
        int modNum = Bomb.GetModuleNames().Count();

        Debug.Log("Mod count: " + modNum);

        char[] charArr = modNum.ToString().ToCharArray();


        List<int> answer = new List<int>();

        foreach (char str in charArr)
        {
            int num = int.Parse("" + str) % 4;

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
                    Debug.LogFormat("[Cruel Simpleton #{0}] Expecting: -... --- -...", ModuleId);
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
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
   }
}
