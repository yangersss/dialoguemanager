using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ink.Runtime;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    public InputManager inputman;

    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.08f;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject continueIcon;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private Animator portraitAnimator;
    

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    
    private Animator layoutAnimator;
    private TextMeshProUGUI[] choicesText;


    private Story currentStory;
    private bool dialogueIsPlaying;
    private bool canContinueToNextLine = false;
    private Coroutine displayLineCoroutine;
    private bool canSkip = false;
    private bool submitSkip;

    private bool testContinueDown = false;

    private static DialogueManager instance;

    //constants for tag keys (good practice for if keys ever change)
    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";

    private void Awake() {
        if (instance != null) {
            Debug.LogWarning("Found more than one Dialogue manager in the scene");
        }
        instance = this;
    }

    public static DialogueManager GetInstance() {
        return instance;
    }

    private void Start() {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        
        //get the layout animator
        layoutAnimator = dialoguePanel.GetComponent<Animator>();

        // get all of the choices text
        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices) {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    private void Update() {
        if (inputman.GetSubmitPressed()){
            testContinueDown = true;
            testContinueDown = false;
        }

        if (inputman.GetSubmitPressed()){
            submitSkip = true;
        }
        
        if (!dialogueIsPlaying) { //return right away if dialogue isn't playing
            return;
        }

        //handle continuing to the next line in the dialogue when submit is pressed
        if (canContinueToNextLine && 
        inputman.GetSubmitPressed()){
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON){
        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);

        // reset portrait, layout, and speaker
        displayNameText.text = "???";
        portraitAnimator.Play("default");
        layoutAnimator.Play("right");

        ContinueStory();
    }

    private IEnumerator ExitDialogueMode(){
        yield return new WaitForSeconds(0.2f);
        
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
    }

    private void ContinueStory(){
        if (currentStory.canContinue){
            // set text for the current dialogue line
            // if last line of dialogue is typing and we continue, skip to next line
            if (displayLineCoroutine != null){
                StopCoroutine(displayLineCoroutine);
            }
            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue()));

            // handle tags
            HandleTags(currentStory.currentTags);
        }
        else{
            StartCoroutine(ExitDialogueMode());
        }
    }

    // instead of showing string all at once, make coroutine that dispalys one letter at a time
    private IEnumerator DisplayLine(string line){
        // empty the dialogue text
        dialogueText.text = "";
        //hide items while text is typing
        continueIcon.SetActive(false);
        HideChoices();

        submitSkip = false;
        canContinueToNextLine = false;
        bool isAddingRichTextTag = false;

        StartCoroutine(CanSkip());

        // display each letter one at a time
        foreach(char letter in line.ToCharArray()){
            //if the submit button is pressed, finish up displaying the line right away
            if (testContinueDown){
                submitSkip = false;
                dialogueText.text = line;
                break;
            }

            // check for rich text tag, if found, add it without waiting
            if (letter == '<' || isAddingRichTextTag){
                isAddingRichTextTag = true;
                dialogueText.text += letter;
                if (letter == '>'){
                    isAddingRichTextTag = false;
                }
            }
            //if not rich text, add the next letter and wait a small time
            else{
                dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        //actions to take after the entire line has finished
        continueIcon.SetActive(true);
        // display choices, if any, for this dialogue line
        DisplayChoices();
        canContinueToNextLine = true;
        canSkip = false;
    }

    //loops thru choices and inactivates them
    private void HideChoices() {
        foreach (GameObject choiceButton in choices){
            choiceButton.SetActive(false);
        }
    }

    private IEnumerator CanSkip(){
        canSkip = false; //make sure var is false
        yield return new WaitForSeconds(0.5f);
        canSkip = true;
    }

    //gives stringlist of all tags for current dialogue
    private void HandleTags(List<string> currentTags){
        //loop through each tag and handle it accordingly
        foreach(string tag in currentTags){
            //parse the tag
            string[] splitTag = tag.Split(':');
            //defensive programming check
            if (splitTag.Length != 2){
                Debug.LogError("Tag could not be appropriately parse: " + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            //handle the tag
            switch (tagKey){
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    layoutAnimator.Play(tagValue);
                    break;
                default:
                    Debug.LogWarning("Tag came in but is not currently being handled: " + tag);
                    break;
            }
        }    
    }

    private void DisplayChoices(){
        List<Choice> currentChoices = currentStory.currentChoices;
        //returns list of choice objects from currentchoices
        if (currentChoices.Count > choices.Length){ //defensive programming check to make sure our UI can support the number of choices coming in
            Debug.LogError("More choices were given than the UI can support. Number of choices given: "
             + currentChoices.Count);
        }

        int index = 0;
        // enable and initialize the choices up to the amount of choices for this line of dialogue
        foreach(Choice choice in currentChoices){
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }
        // go through the remaining choices the UI supports and make sure they're hidden
        for (int i = index; i < choices.Length; i++){
            choices[i].gameObject.SetActive(false);
        }

        StartCoroutine(SelectFirstChoice());
    }

    private IEnumerator SelectFirstChoice(){
        // event system requres we clear it first, then wait
        // for at least one frame before we set the current selected object
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex){
        if (canContinueToNextLine){
            currentStory.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
        
    }
}