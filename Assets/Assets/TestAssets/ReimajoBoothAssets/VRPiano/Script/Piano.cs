///Keep those commented out in the live build, since debug logs are performance heavy
//#define TEST_DEBUG //I use this while developing to make the piano more efficient in terms of CPU frametime
//#define BONE_DEBUG //I use this while developing to check if the piano bones are mapped correctly to the MIDI keys
//#define MENU_DEBUG //I use this while developing to debug the touch menu
//#define UDON_VOLUME_CURVES //Makes the key audio to reduce volume over time via Udon which was replaced by a more performant
//workaround until pedals might be implemented one day
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
/// <summary>
/// Script from Reimajo purchased at https://reimajo.booth.pm/
/// If you have any issues, please contact me on Discord (Reimajo#1009) or Booth or Twitter https://twitter.com/ReimajoChan
/// </summary>
namespace ReimajoBoothAssets
{
    /// <summary>
    /// Script can be a single time or multiple times in the world. The piano must be axis alligned in order to work.
    /// The piano can be scaled, but only in editor (not on runtime) and must be scaled only on the root object (named "*__SCALE_ONLY_HERE").
    /// The piano must stay at one position and cannot move in order to work.
    /// You should NOT have "Synchronize Position" enabled for this script (should be default off anyway unless you set it manually).
    /// You should NOT have "Transfer Ownership on Collision" enabled for this script.
    /// </summary>
    public class Piano : MonoBehaviour
    {
        #region Debug
#if TEST_DEBUG
        private int _debugFrameCount = 0;
        private int _isHeldDebugCount = 0;
        private int _wasInBoundDebugCount = 0;
        private int _wasTriggeredDebugCount = 0;
#endif
        #endregion Debug
        #region PlayerCalibrationAPI
        /// <summary>
        /// Avatar height in meter (of localPlayer), can be set by an external script for better usability of the piano. 
        /// Afterwards, OnAvatarChanged() must be called on this script.
        /// One such script that does all of that is my Player Calibration Script which I sell on my booth page (https://booth.pm/en/items/2753511)
        /// </summary>
        //[HideInInspector] <-- This can be uncommented when the player calibration script is in the scene
        [Tooltip("Avatar height in meter (of localPlayer), can be set by an external script for better usability of the piano. Afterwards, OnAvatarChanged() must be called on this script. One such script that does all of that is my AvatarCalibrationScript which I sell on my booth page.")]
        public float _avatarHeight = 1.3f;
        /// <summary>
        /// Relevant bone from the left hand (of localPlayer), can be set by an external script for better usability of the piano accross all players. 
        /// One such script is my Player Calibration Script which I sell on my booth page (https://booth.pm/en/items/2753511)
        /// </summary>
        //[HideInInspector] <-- This can be uncommented when the player calibration script is in the scene
        [Tooltip("Relevant bone from the left hand (of localPlayer), can be set by an external script for better usability of the piano accross all players. One such script is my AvatarCalibrationScript which I sell on my booth page.")]
        public HumanBodyBones _leftIndexBone = HumanBodyBones.LeftIndexDistal;
        /// <summary>
        /// Relevant bone from the right hand (of localPlayer), can be set by an external script for better usability of the piano accross all players. 
        /// One such script is my Player Calibration Script which I sell on my booth page (https://booth.pm/en/items/2753511)
        /// </summary>
        //[HideInInspector] <-- This can be uncommented when the player calibration script is in the scene
        [Tooltip("Relevant bone from the right hand (of localPlayer), can be set by an external script for better usability of the piano accross all players. One such script is my AvatarCalibrationScript which I sell on my booth page.")]
        public HumanBodyBones _rightIndexBone = HumanBodyBones.RightIndexDistal;
        #endregion PlayerCalibrationAPI
        #region SerializedFields
        [Space(10)]
        // ------------- objects for testing in editor -----------------
        /// <summary>
        /// Only used to test the piano in editor
        /// </summary>
        [SerializeField, Tooltip("Can be used to test the piano in editor, isn't used in game")]
        private Transform _fakeHand;
        [Space(10)]
        // ------------- Materials that are assigned on runtime -----------------
        [SerializeField, Tooltip("Material is assigned to a block while it's being hit correctly")]
        private Material _blockMaterialWhenHitting;
        [Space(10)]
        // ------------- blueprints of objects that are instanciated at runtime -----------------
        [SerializeField]
        private GameObject _songLabelBlueprint;
        [SerializeField]
        private GameObject _whiteGameNoteBlueprint;
        [SerializeField]
        private GameObject _blackGameNoteBlueprint;
        [SerializeField]
        private GameObject _whiteNoteLabelBlueprint;
        [SerializeField]
        private GameObject _blackNoteLabelBlueprint;
        [SerializeField]
        private GameObject _audioSourceBlueprint;
        [Space(10)]
        // ------------- display root objects to toggle them -----------------
        [SerializeField]
        private GameObject _gameStatsDisplay;
        [SerializeField]
        private GameObject _customSongMenu;
        [SerializeField]
        private GameObject _menuDisplayObj;
        [Space(10)]
        // ------------- menu text items -----------------
        [SerializeField]
        private Text _currentGameModeDisplay;
        [SerializeField]
        private Text _menuToggleText;
        [SerializeField]
        private Text _menuShownotesText;
        [SerializeField]
        private Text _menuNetworkmodeText;
        [SerializeField]
        private Text _menuFingermodeText;
        [SerializeField]
        private Text _menuSlipmodeText;
        [SerializeField]
        private Text _levelErrorText;
        [SerializeField]
        private InputField _levelInputField;
        [Space(10)]
        // ------------- armature bones from the piano -----------------
        /// <summary>
        /// Armature root bone
        /// </summary>
        [SerializeField]
        private Transform _blackKeysRoot;
        [SerializeField]
        private Transform _whiteKeysRoot;
        /// <summary>
        /// Bone to open/close the lid, currently not in use due to VRChat sync constraints - will change in the future
        /// </summary>
        [SerializeField]
        private Transform _lidBone;
        [Space(10)]
        // ------------- box colliders to read their bounds at start, used to minimize CPU frametime -----------------
        /// <summary>
        /// Areas in which piano keys can be pressed
        /// </summary>
        [SerializeField]
        private BoxCollider _allBlackKeysBoxCollider;
        [SerializeField]
        private BoxCollider _allWhiteKeysBoxCollider;
        [SerializeField]
        private BoxCollider _menuToggleBoxCollider;
        [SerializeField]
        private BoxCollider _menuBoxCollider;
        [Space(10)]
        // ------------- reference points in world space -----------------
        /// <summary>
        /// Reference for the rightmost key border
        /// </summary>
        [SerializeField]
        private Transform _whiteZeroKeyPlane;
        [SerializeField]
        private Transform _keyRotationZeroRef;
        [SerializeField]
        private Transform _menuRefPoint;
        [Space(10)]
        // ------------- parent transforms to attach instanciated objects as childs -----------------
        [SerializeField]
        private Transform _blockSpawnParent;
        [SerializeField]
        private Transform _blockSpawnHeightRef;
        [SerializeField]
        private Transform _blackblockHitHeightRef;
        [SerializeField]
        private Transform _whiteblockHitHeightRef;
        [Space(10)]
        // ------------- game menu text items -----------------
        [SerializeField]
        private Text _gameUiHeader;
        [SerializeField]
        private Text _gameUiDescription;
        [SerializeField]
        private Text _gameUiWrongHits;
        [SerializeField]
        private Text _gameUiMissedHits;
        [SerializeField]
        private Text _gameUiGoodHits;
        [SerializeField]
        private Text _gameUiPrecision;
        [SerializeField]
        private Text _gameUiScore;
        /// <summary>
        /// Reserved to display game modifiers in the future. This is a planned feature.
        /// </summary>
        [SerializeField]
        private Text _gameUiMode;
        #endregion SerializedFields
        #region Settings
        [Space(10)]
        /// <summary>
        /// Standard gap between two note blocks in game mode for a piano of scale 1, will be scaled with the piano
        /// </summary>
        [SerializeField, Tooltip("Standard gap between two note blocks in game mode for a piano of scale 1, will be scaled with the piano")]
        private float NOTE_GAP_DIVIDER = 0.7f;
        /// <summary>
        /// Speed at which a note will fall down in the game mode in meters per second and piano size, set it as if the piano had scale 1
        /// </summary>
        [SerializeField, Tooltip("Speed at which a note will fall down in the game mode in meters per second")]
        private float DEFAULT_BLOCK_SPEED_MS = 0.15f;
        /// <summary>
        /// Maximum distance to a player of 1.3m in size in meters after which the script will stop running.
        /// This value is scaled up/down with avatar size and piano size, set it as if the piano had scale 1
        /// </summary>
        [SerializeField, Tooltip("Maximum distance to a player of 1.5m in size in meters after which the script will stop running. This value is scaled up/down with avatar size")]
        private float DEFAULT_INTERACTION_DISTANCE = 3f; //for standard sized avatars
        /// <summary>
        /// Distance from finger bone to skin for a standard sized avatar (1.3m)
        /// This value scales up/down with avatar size
        /// </summary>
        [SerializeField, Tooltip("Distance from finger bone to skin for a standard sized avatar (1.3m). This value scales up/down with avatar size")]
        private float FINGER_THICKNESS_DEFAULT = 0.02f;
        /// <summary>
        /// Distance from head bone to skin for a standard sized avatar (1.3m)
        /// This value scales up/down with avatar size
        /// </summary>
        [SerializeField, Tooltip("Distance from head bone to skin for a standard sized avatar (1.3m). This value scales up/down with avatar size")]
        private float HEAD_THICKNESS_DEFAULT = 0.25f;
        /// <summary>
        /// Distance from feet bone to skin for a standard sized avatar (1.3m)
        /// This value scales up/down with avatar size
        /// </summary>
        [SerializeField, Tooltip("Distance from feet bone to skin for a standard sized avatar (1.3m). This value scales up/down with avatar size")]
        private float FOOT_THICKNESS_DEFAULT = 0.05f;
        /// <summary>
        /// Speed in degrees per second at which a key moves back after being pressed, higher is faster, 8 is default value
        /// </summary>
        [SerializeField, Tooltip("Speed in degrees per second at which a key moves back after being pressed, higher is faster, 8 is default value")]
        private float MOVE_BACK_SPEED = 8f;
        /// <summary>
        /// Maximum distance after which audio is no longer audible and no code will run
        /// </summary>
        [SerializeField, Tooltip("Maximum distance after which audio is no longer audible and no code will run")]
        private float MAX_AUDIO_DISTANCE = 15f;
        /// <summary>
        /// (Maximum) Time in seconds of one note sound. Needs to match the length of the corresponding animation. Do not change without changing the animation!
        /// </summary>
        [SerializeField, Tooltip("(Maximum) Time in seconds of one note sound. Needs to match the length of the corresponding animation. Do not change without changing the animation!")]
        private float PLAY_TIME = 4f;
        /// <summary>
        /// Maximum number of parallel audio sources. This can be used to avoid performance spikes. 100-120 is recommended.
        /// </summary>
        [SerializeField, Tooltip("Maximum number of parallel audio sources = key sounds that can play at once. This can be used to avoid CPU performance spikes. 120 max is recommended, can be set lower to ensure your world doesn't run into performance issues.")]
        private int MAX_AUDIO_SOURCES = 100;
        /// <summary>
        /// Thickness of one white key if the piano has scale 1, will be scaled automatically with piano scale.
        /// </summary>
        private float KEY_THICKNESS_WHITE = 0.0129f;
        /// <summary>
        /// Thickness of one black key if the piano has scale 1, will be scaled automatically with piano scale.
        /// </summary>
        private float KEY_THICKNESS_BLACK = 0.03412f;
        /// <summary>
        /// Must be pre-calculated via editor: Distance to the middle of one key if the piano has scale 1, will be scaled automatically with piano scale.
        /// </summary>
        private float BLACK_KEY_SIZE = 0.4f;
        /// <summary>
        /// Maximum of blocks that can be shown at once during game mode
        /// </summary>
        private const int MAX_BLOCKS_AMOUNT = 250;
        /// <summary>
        /// Vertical size of one block before scaling
        /// </summary>
        private float VERTICAL_BLOCK_SIZE = 0.1f;
        /// <summary>
        /// Minimum time in seconds between 2 key down events of the same key, to de-bounce this physical key https://www.quora.com/What-is-key-debounce
        /// </summary>
        private const float MINIMUM_TIME_BETWEEN_KEYPRESS = 0.1f;
        #endregion Settings
        #region PrivateVariables
        private float _blockSpeedMs;
        private bool _moveBlocks;
        private bool _keyStopsSong;
        private bool _keySelectsSong;
        private bool _songLabelsShown;
        private bool _watchGame = false;
        private bool _forwardKeysToGameMode = false;
        private int _currentGameMode = 0;
        private const int GAME_MODE_OFF = 0;
        private const int GAME_MODE_PLAY = 1;
        private const int GAME_MODE_PREVIEW = 2;
        private const int GAME_MODE_CUSTOM_SONG = 3;
        private const int GAME_MODE_CUSTOM_SONG_PREVIEW = 4;
        private const int GAME_MODE_WATCH = 5;
        private const int GAME_MODE_END = 6; //just to mark the end of the "enum array".
        /// <summary>
        /// Variables for the song library
        /// </summary>
        private string[] _songMMLLibrary;
        private string[] _songNameLibrary;
        private GameObject[] _songLabels;
        private bool _songLibraryIsSetup;
        /// <summary>
        /// Bone detection areas
        /// </summary>
        private Bounds _allBlackKeysBounds;
        private Bounds _allWhiteKeysBounds;
        private Bounds _menuToggleArea;
        private Bounds _menuArea;
        /// <summary>
        /// The (other) furthest bones from the current avatar. First (index) bones are declared further above.
        /// </summary>
        private HumanBodyBones _fingerbone2r = HumanBodyBones.RightLittleDistal;
        private HumanBodyBones _fingerbone3r = HumanBodyBones.RightMiddleDistal;
        private HumanBodyBones _fingerbone4r = HumanBodyBones.RightRingDistal;
        private HumanBodyBones _fingerbone5r = HumanBodyBones.RightThumbDistal;
        private HumanBodyBones _fingerbone2l = HumanBodyBones.LeftLittleDistal;
        private HumanBodyBones _fingerbone3l = HumanBodyBones.LeftMiddleDistal;
        private HumanBodyBones _fingerbone4l = HumanBodyBones.LeftRingDistal;
        private HumanBodyBones _fingerbone5l = HumanBodyBones.LeftThumbDistal;
        private HumanBodyBones _leftFeetBone = HumanBodyBones.LeftFoot;
        private HumanBodyBones _rightFeetBone = HumanBodyBones.RightFoot;
        private bool _useAllFingerBones = true;
        /// <summary>
        /// Store when we've sent this key over network the last time
        /// </summary>
        private float[] _lastSendTime = new float[88];
        /// <summary>
        /// Store when we've received this key over network the last time
        /// </summary>
        private float[] _lastReceivedTime = new float[88];
        /// <summary>
        /// Maximum distance to a player of 1.5m in size in meters after which the script will stop running.
        /// This value is scaled up/down with avatar size
        /// </summary>
        private float _interactionDistance;
        private float _fingerThickness;
        private float _headThickness;
        private float _footThickness;
        private const int WHITE_KEYS_LENGTH = 52;
        private const int BLACK_KEYS_LENGTH = 36;
        private const int ALL_KEYS_LENGHT = WHITE_KEYS_LENGTH + BLACK_KEYS_LENGTH;
        //Storage arrays for individual key states
        private bool[] _wasInBound;
        private int _wasInBoundCount = 0;
        private bool[] _wasTriggered;
        private float[] _lastTriggerTime;
        private int[] _currentFingerKeyBinding = new int[15]; //amount of all bones!
        private int[] _blackKeyMapping;
        private int[] _blackKeyToMidi;
        private int[] _whiteKeyToMidi;
        // Maximum angle of the keys when pressed    
        private const float _KEY_MAX_ROTATION = 2f;
        private Vector3 _keyProjectionForward;
        // Current angular position of the keys
        private float[] _currentKeyAngle;
        private bool[] _isHeld;
        private int _isHeldCount = 0;
        private bool _isSetup;
        private bool _hasFinishedStart;
        private float _whiteKeySize; //is calculated in script
        /// <summary>
        /// Audio-related variables
        /// </summary>
        private int _amountOfSpawnedAudioSources = 0;
        private float[] _audioSpawnTimes;
        private AudioSource[] _audioSourcesSpawned;
        private bool _useNoSlipMode;
        private bool _showNotes;
        private GameObject[] _noteLabels;
        private float _menuToggleHeight;
        private float _menuHeight;
        private int _currentBoneIndex;
        private bool _receiveNetworkEvents;
        /// <summary>
        /// If any menu key is currently being pressed
        /// </summary>
        private bool _menuKeyPressed;
        private bool _menuToggleKeyPressed;
        private bool _menuOpen;
        /// <summary>
        /// Wheter or not it's currently playing a level
        /// </summary>
        private bool _runLevel;
        private bool _pendingLevel;
        private bool _songLabelsInstantiated;
        private bool _fingerWasAboveMenu = false;
        private bool _fingerWasAboveMenuToggle = false;
        private bool _noteLabelsInstantiated;
        #endregion PrivateVariables
        #region MMLParser
        private bool _hasMelody;
        private bool _hasHarmony1;
        private bool _hasHarmony2;
        private bool _hasSong;
        private int[] _track0Notes;
        private float[] _track0NotesTiming;
        private int[] _track1Notes;
        private float[] _track1NotesTiming;
        private int[] _track2Notes;
        private float[] _track2NotesTiming;
        private int[] _track3Notes;
        private float[] _track3NotesTiming;
        private string _trackError = "";
        private int _unparsableNotesCount = 0;
        private const string _BLOCK_SPEED_INDICATOR = "speed";
        private const string _MML_INDICATOR = "mml@";
        private const string _ALL_NUMBERS = "0123456789";
        private float _timeNextTrack0Note;
        private int _runningTrack0NoteIndex;
        private float _timeNextTrack1Note;
        private int _runningTrack1NoteIndex;
        private float _timeNextTrack2Note;
        private int _runningTrack2NoteIndex;
        private float _timeNextTrack3Note;
        private int _runningTrack3NoteIndex;
        private float _currentBPM = 120f;
        private int _currentOcatave = 4;
        private float _currentDefaultNoteLenght = 4f;
        private bool _debugParser = false;
        /// <summary>
        /// Parses an MML track
        /// MML@ = start of a song, optional
        /// Syntax: MML@[Melody],[Harmony 1],[Harmony 2],[Song];
        /// </summary>
        private bool ParseMML(string input)
        {
            //reset all tracks
            ResetAllTracks();
            //declare default song start index
            int songStart = 0;
            //we just process lowerchars
            input = input.ToLower();
            //check where the (first) song ends, only this one is being processed
            int songEnd = input.IndexOf(';');
            //there might be no song ending character, in which case we just set the end of the string
            if (songEnd == -1)
                songEnd = input.Length - 1;
            //check if there is a note speed indicator
            int blockSpeedStartIndex = input.IndexOf(_BLOCK_SPEED_INDICATOR.ToLower());
            //if there is none, set the start to 0, else set it to after 'speed'
            if (blockSpeedStartIndex != -1)
            {
                int blockSpeed = GetNextInteger(index: blockSpeedStartIndex + _BLOCK_SPEED_INDICATOR.Length - 1, input.ToCharArray());
                if (blockSpeed == -1)
                {
                    _trackError = $"[Piano] Could not read or interpret block speed value at position {blockSpeedStartIndex}";
                    return false;
                }
                songStart = blockSpeedStartIndex + _BLOCK_SPEED_INDICATOR.Length + blockSpeed.ToString().Length; //put cursor behind this number
                _blockSpeedMs = (float)blockSpeed / 1000f;
                Debug.Log($"[Piano] Block speed was set to {_blockSpeedMs} (value was {blockSpeed})");
            }
            else
            {
                _blockSpeedMs = DEFAULT_BLOCK_SPEED_MS;
            }
            //check if there is an MML indicator
            int mmlIndicatorStartIndex = input.IndexOf(_MML_INDICATOR);
            //if there is none, set the start to 0, else set it to after @MML if @MML followed after the block speed indicator
            if (mmlIndicatorStartIndex != -1 && mmlIndicatorStartIndex > blockSpeedStartIndex)
                songStart += _MML_INDICATOR.Length;
            //extract just the first song now
            string song1 = input.Substring(songStart, songEnd - songStart);
            //split into tracks
            string[] tracks = song1.Split(',');
            //check which tracks are included
            switch (tracks.Length)
            {
                case 1:
                    _hasMelody = true;
                    break;
                case 2:
                    _hasMelody = true;
                    _hasHarmony1 = true;
                    break;
                case 3:
                    _hasMelody = true;
                    _hasHarmony1 = true;
                    _hasHarmony2 = true;
                    break;
                default:
                    if (tracks.Length > 4)
                        Debug.LogError($"Input has {tracks.Length} tracks, but only the first 4 tracks will be played since this is the maximum amount that we support.");
                    _hasMelody = true;
                    _hasHarmony1 = true;
                    _hasHarmony2 = true;
                    _hasSong = true;
                    break;
            }
            //parse all tracks that are not empty. There is always at least 1 track if we reach this line.
            string nextTrack = tracks[0].Trim();
            //parse first track (melody)
            if (_hasMelody)
            {
                if (nextTrack.Length == 0)
                {
                    _hasMelody = false;
                    Debug.Log($"[Piano] Track 0 (melody) is empty and won't be parsed.");
                }
                else if (!ParseSingleMMLTrack(trackID: 0, nextTrack))
                    return false;
                else if (GetNoteArrayLenght(0) == 0)
                {
                    _hasMelody = false;
                    Debug.Log($"[Piano] Track 0 (melody) has no note and won't be played.");
                }
                else
                    Debug.Log($"[Piano] Parsed track 0 (melody) successfully. Found {GetNoteArrayLenght(0)} notes.");
            }
            if (tracks.Length == 1)
                return true; //return true to indicate the success
                             //parse second track (harmony 1)
            nextTrack = tracks[1].Trim();
            if (_hasHarmony1)
            {
                if (nextTrack.Length == 0)
                {
                    _hasHarmony1 = false;
                    Debug.Log($"[Piano] Track 1 (harmony 1) is empty and won't be parsed.");
                }
                else if (!ParseSingleMMLTrack(trackID: 1, nextTrack))
                    return false;
                else if (GetNoteArrayLenght(1) == 0)
                {
                    _hasHarmony1 = false;
                    Debug.Log($"[Piano] Track 1 (harmony 1) has no note and won't be played.");
                }
                else
                    Debug.Log($"[Piano] Parsed track 1 (harmony 1) successfully. Found {GetNoteArrayLenght(1)} notes.");
            }
            if (tracks.Length == 2)
                return true; //return true to indicate the success
                             //parse second track (harmony 2)
            nextTrack = tracks[2].Trim();
            if (_hasHarmony2 && nextTrack.Length > 0)
            {
                if (nextTrack.Length == 0)
                {
                    _hasHarmony2 = false;
                    Debug.Log($"[Piano] Track 2 (harmony 2) is empty and won't be parsed.");
                }
                else if (!ParseSingleMMLTrack(trackID: 2, nextTrack))
                    return false;
                else if (GetNoteArrayLenght(2) == 0)
                {
                    _hasMelody = false;
                    Debug.Log($"[Piano] Track 2 (harmony 2) has no note and won't be played.");
                }
                else
                    Debug.Log($"[Piano] Parsed track 2 (harmony 2) successfully. Found {GetNoteArrayLenght(2)} notes.");
            }
            if (tracks.Length == 3)
                return true; //return true to indicate the success
                             //parse second track (song)
            nextTrack = tracks[3].Trim();
            if (_hasSong && nextTrack.Length > 0)
            {
                if (nextTrack.Length == 0)
                {
                    _hasSong = false;
                    Debug.Log($"[Piano] Track 3 (song) is empty and won't be parsed.");
                }
                else if (!ParseSingleMMLTrack(trackID: 3, nextTrack))
                    return false;
                else if (GetNoteArrayLenght(3) == 0)
                {
                    _hasMelody = false;
                    Debug.Log($"[Piano] Track 3 (song) has no note and won't be played.");
                }
                else
                    Debug.Log($"[Piano] Parsed track 3 (song) successfully. Found {GetNoteArrayLenght(3)} notes.");
            }
            //return true to indicate the success
            return true;
        }
        /// <summary>
        /// Adds the note of a track at the specified index
        /// </summary>
        private void SetNote(int trackID, int note, int index, float noteTiming)
        {
            if (_debugParser)
                Debug.Log($"[Piano] Set note {note} to track {trackID} with timing {noteTiming}");
            switch (trackID)
            {
                case 0:
                    _track0Notes[index] = note;
                    _track0NotesTiming[index] = noteTiming;
                    break;
                case 1:
                    _track1Notes[index] = note;
                    _track1NotesTiming[index] = noteTiming;
                    break;
                case 2:
                    _track2Notes[index] = note;
                    _track2NotesTiming[index] = noteTiming;
                    break;
                case 3:
                    _track3Notes[index] = note;
                    _track3NotesTiming[index] = noteTiming;
                    break;
                default:
                    Debug.LogError($"[Piano] Track {trackID} does not exist in AddNote()");
                    break;
            }
        }
        /// <summary>
        /// Returns the length of the corresponding track arrays
        /// </summary>
        private int GetNoteArrayLenght(int trackID)
        {
            switch (trackID)
            {
                case 0:
                    return _track0Notes.Length;
                case 1:
                    return _track1Notes.Length;
                case 2:
                    return _track2Notes.Length;
                case 3:
                    return _track3Notes.Length;
                default:
                    Debug.LogError($"[Piano] Track {trackID} does not exist in GetNoteArrayLenght()");
                    return 0;
            }
        }
        /// <summary>
        /// Sets up the track arrays
        /// </summary>
        private void SetupNoteArray(int trackID, int length)
        {
            switch (trackID)
            {
                case 0:
                    _track0Notes = new int[length];
                    _track0NotesTiming = new float[length];
                    break;
                case 1:
                    _track1Notes = new int[length];
                    _track1NotesTiming = new float[length];
                    break;
                case 2:
                    _track2Notes = new int[length];
                    _track2NotesTiming = new float[length];
                    break;
                case 3:
                    _track3Notes = new int[length];
                    _track3NotesTiming = new float[length];
                    break;
                default:
                    Debug.LogError($"[Piano] Track {trackID} does not exist in SetupNoteArray()");
                    break;
            }
        }
        /// <summary>
        /// Returns the note of a track at the specified index
        /// </summary>
        private int GetNote(int trackID, int index)
        {
            switch (trackID)
            {
                case 0:
                    return _track0Notes[index];
                case 1:
                    return _track1Notes[index];
                case 2:
                    return _track2Notes[index];
                case 3:
                    return _track3Notes[index];
                default:
                    Debug.LogError($"[Piano] Track {trackID} does not exist in GetNote()");
                    return 0;
            }
        }
        /// <summary>
        /// Returns the note timing of a track at the specified index
        /// </summary>
        private float GetNoteTiming(int trackID, int index)
        {
            switch (trackID)
            {
                case 0:
                    return _track0NotesTiming[index];
                case 1:
                    return _track1NotesTiming[index];
                case 2:
                    return _track2NotesTiming[index];
                case 3:
                    return _track3NotesTiming[index];
                default:
                    Debug.LogError($"[Piano] Track {trackID} does not exist in GetNoteTiming()");
                    return 0;
            }
        }
        /// <summary>
        /// Reset all tracks
        /// </summary>
        private void ResetAllTracks()
        {
            _pendingLevel = false;
            _runLevel = false;
            _unparsableNotesCount = 0;
            _currentBPM = 120f;
            _currentOcatave = 4;
            _currentDefaultNoteLenght = 4f;
            _trackError = "";
            _hasMelody = false;
            _hasHarmony1 = false;
            _hasHarmony2 = false;
            _hasSong = false;
            _track0Notes = null;
            _track0NotesTiming = null;
            _track1Notes = null;
            _track1NotesTiming = null;
            _track2Notes = null;
            _track2NotesTiming = null;
            _track3Notes = null;
            _track3NotesTiming = null;
        }
        /*
        INPORTANT: we only use the lowercase version in here!
        T or t = speed in bpm, followed by a number, can change during the song
        O or o = octave, is 4 by default at start
        > = plus one octave for the following notes
        < = minus one octave for the following notes
        V or v = volume from 0 to 15
        L or l = default length for the following notes, is 4 by default at start, followed by a number
        1,2,3,4,8,16,32,64 note length
        a, b, c, d, e, f, g Notes
# or + = sharp note, always noted after the letter
        - = flat note, always noted after the letter
        r = rest note (pause)
        & = extends a note, e.g.e-1&e-1&e-1&e-1 or c&c&c
        . = extend a preceeding number by 50% of it's length 
        N or n = raw note, value of 0 to 127
        */
        /// <summary>
        /// Parses a single track and fills in the note and timing array
        /// </summary>
        private bool ParseSingleMMLTrack(int trackID, string input)
        {
            //get rid of redundant sharpness symbol, also extend track by two character to avoid exceptions
            input = input.Replace('#', '+') + "  ";
            Debug.Log($"[Piano] MML track {trackID} content: '{input}'");
            float secondsPerFullNote = ConvertToSecondsPerNote(_currentBPM);
            Debug.Log($"[Piano] MML track {trackID} has {secondsPerFullNote} seconds per full note ({_currentBPM} bpm) before parsing the track. This might change.");
            int currentOctaveID = ConvertToOctaveID(_currentOcatave);
            Debug.Log($"[Piano] MML track {trackID} starts in octave {_currentOcatave} before parsing the track. This might change.");
            char[] trackChars = input.ToCharArray();
            int noteCount = GetNoteCount(trackChars);
            //initialize the note arrays
            SetupNoteArray(trackID, noteCount);
            //stop here if there is no note in the track
            if (noteCount == 0)
                return true;
            //parse all characters of the track
            bool extendNextNote = false;
            char currentChar;
            int currentNoteIndex = 0;
            string fullNoteString;
            int noteID;
            float timing;
            const int OTHER_VALUE = 0;
            const int NOTE_VALUE = 1;
            const int TIME_VALUE = 2;
            const int VOLUME_VALUE = 3;
            const int LENGHT_VALUE = 4;
            int lastValueIndex = OTHER_VALUE; //to remember what we last found in this track
            for (int i = 0; i < trackChars.Length - 2; i++)
            {
                currentChar = trackChars[i];
                switch (currentChar)
                {
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'r':
                    case 'p':
                        lastValueIndex = NOTE_VALUE; //for a note
                                                     //convert to the full string representation
                        if (trackChars[i + 1] == '+')
                        {
                            fullNoteString = ConvertToSharpNote(noteName: currentChar);
                            i++; //skip this char
                        }
                        else if (trackChars[i + 1] == '-')
                        {
                            fullNoteString = ConvertToFlatNote(noteName: currentChar);
                            i++; //skip this char
                        }
                        else
                            fullNoteString = currentChar.ToString();
                        //convert to a note index
                        noteID = ConvertToNoteID(currentOctaveID, fullNoteString);
                        if (noteID == -1) //this happens when the converter failed
                        {
                            _trackError = $"[Piano] Could not parse note '{fullNoteString}' at position {i} in track {trackID}";
                            return false;
                        }
                        if (_debugParser)
                            Debug.Log($"[Piano] Found note: {fullNoteString}");
                        int noteLenght = GetNextInteger(index: i, trackChars);
                        if (noteLenght == -1) //if there is no length specified, we apply the current default value
                        {
                            if (_debugParser)
                                Debug.Log($"[Piano] Note length: {_currentDefaultNoteLenght} (current default)");
                            timing = secondsPerFullNote / _currentDefaultNoteLenght;
                        }
                        else if (CheckNoteLenght(noteLenght))
                        {
                            if (_debugParser)
                                Debug.Log($"[Piano] Note length: {noteLenght}");
                            timing = secondsPerFullNote / (float)noteLenght;
                            i += noteLenght.ToString().Length; //put cursor behind this number
                        }
                        else
                        {
                            _trackError = $"[Piano] Could not read or interpret note length at position {i} in track {trackID}";
                            return false;
                        }
                        if (extendNextNote)
                        {
                            extendNextNote = false;
                            if (!ExtendLastNote(trackID, currentNoteIndex, noteID, timing))
                                return false;
                        }
                        else
                        {
                            SetNote(trackID, noteID, currentNoteIndex, timing);
                            currentNoteIndex++;
                        }
                        break;
                    case 'o':
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        _currentOcatave = GetNextInteger(index: i, trackChars);
                        if (_debugParser)
                            Debug.Log($"[Piano] Changed octave to {_currentOcatave}");
                        currentOctaveID = ConvertToOctaveID(_currentOcatave);
                        if (currentOctaveID == -1)
                        {
                            _trackError = $"[Piano] Could not accept octave '{_currentOcatave}' at position {i} in track {trackID}";
                            return false;
                        }
                        i += currentOctaveID.ToString().Length; //put cursor behind this number
                        break;
                    case '<':
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        _currentOcatave--;
                        if (_debugParser)
                            Debug.Log($"[Piano] Decreased octave to {_currentOcatave}");
                        currentOctaveID = ConvertToOctaveID(_currentOcatave);
                        if (currentOctaveID == -1)
                        {
                            _trackError = $"[Piano] Could not accept octave decreasement to octave '{_currentOcatave}' at position {i} in track {trackID} since it's out of range 1-7";
                            return false;
                        }
                        break;
                    case '>':
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        _currentOcatave++;
                        if (_debugParser)
                            Debug.Log($"[Piano] Increased octave to {_currentOcatave}");
                        currentOctaveID = ConvertToOctaveID(_currentOcatave);
                        if (currentOctaveID == -1)
                        {
                            _trackError = $"[Piano] Could not accept octave increasement to octave '{_currentOcatave}' at position {i} in track {trackID} since it's out of range 1-7";
                            return false;
                        }
                        break;
                    case 'l':
                        lastValueIndex = LENGHT_VALUE; //for a note
                        _currentDefaultNoteLenght = GetNextInteger(index: i, trackChars);
                        if (_debugParser)
                            Debug.Log($"[Piano] Changed default note length to {_currentDefaultNoteLenght}");
                        if (_currentDefaultNoteLenght == -1 || !CheckNoteLenght(_currentDefaultNoteLenght))
                        {
                            _trackError = $"[Piano] Could not read or interpret default note length at position {i} in track {trackID}";
                            return false;
                        }
                        i += _currentDefaultNoteLenght.ToString().Length; //put cursor behind this number
                        break;
                    case '.': //extends the preceeding number by 50% of it's length
                        switch (lastValueIndex)
                        {
                            case NOTE_VALUE:
                                int lastNoteIndex = currentNoteIndex - 1;
                                SetNote(trackID, GetNote(trackID, lastNoteIndex), lastNoteIndex, 1.5f * GetNoteTiming(trackID, lastNoteIndex));
                                if (_debugParser)
                                    Debug.Log($"[Piano] Extended last note time by 50%");
                                break;
                            case TIME_VALUE:
                                _currentBPM *= 1.5f;
                                secondsPerFullNote = ConvertToSecondsPerNote(_currentBPM);
                                if (_debugParser)
                                {
                                    Debug.Log($"[Piano] Extended bpm by dot to {_currentBPM} ---------------BPM CHANGE------------");
                                    Debug.Log($"[Piano] Track will have {secondsPerFullNote} seconds per full note now.");
                                }
                                break;
                            case VOLUME_VALUE:
                                //volume values are not supported so we will just ignore them
                                break;
                            case LENGHT_VALUE:
                                _currentDefaultNoteLenght *= 1.5f;
                                if (_debugParser)
                                    Debug.Log($"[Piano] Extended default note length by 50% to {_currentDefaultNoteLenght}");
                                break;
                            default:
                                _trackError = $"[Piano] Found a '.' on position {i} but didn't know what to do with it (Track {trackID})";
                                return false;
                        }
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        break;
                    case '&': //extends a note by another note
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        if (_debugParser)
                            Debug.Log($"[Piano] Will extend the last note by the following note");
                        if (currentNoteIndex < 1)
                        {
                            _trackError = $"[Piano] A track can't start with a '&' before the first note (Track {trackID})";
                            return false;
                        }
                        extendNextNote = true;
                        break;
                    case 't': //speed in bpm
                        lastValueIndex = TIME_VALUE; //for a note
                        _currentBPM = GetNextInteger(index: i, trackChars);
                        if (_debugParser)
                            Debug.Log($"[Piano] Read and set bpm to {_currentBPM} ---------------BPM CHANGE------------");
                        if (_currentBPM == -1)
                        {
                            _trackError = $"[Piano] Could not read or interpret bpm value at position {i} in track {trackID}";
                            return false;
                        }
                        i += _currentBPM.ToString().Length; //put cursor behind this number
                        secondsPerFullNote = ConvertToSecondsPerNote(_currentBPM);
                        if (_debugParser)
                            Debug.Log($"[Piano] Track will have {secondsPerFullNote} seconds per full note now.");
                        break;
                    case 'v': //volume (we ignore this, but we need to shift the cursor)
                        lastValueIndex = VOLUME_VALUE; //for a note
                        int volume = GetNextInteger(index: i, trackChars);
                        if (_debugParser)
                            Debug.Log($"[Piano] Read and set volume to {volume}");
                        if (volume == -1)
                        {
                            _trackError = $"[Piano] Could not read or interpret volume value at position {i} in track {trackID}";
                            return false;
                        }
                        i += volume.ToString().Length; //put cursor behind this number
                        break;
                    case 'n': //a raw note
                        lastValueIndex = NOTE_VALUE; //for a note
                        int rawNoteMIDI = GetNextInteger(index: i, trackChars);
                        if (_debugParser)
                            Debug.Log($"[Piano] Read raw note (assuming it's midi): {rawNoteMIDI}");
                        if (rawNoteMIDI == -1)
                        {
                            _trackError = $"[Piano] Could not read or interpret raw note value value at position {i} in track {trackID}";
                            return false;
                        }
                        i += rawNoteMIDI.ToString().Length; //put cursor behind this number
                                                            //performs an expensive search for the corresponding bone
                        noteID = ConvertMidiNoteToKeyID(rawNoteMIDI);
                        if (noteID == -1)
                        {
                            _unparsableNotesCount++;
                            Debug.LogError($"[Piano] Soft error in MML parser: Midi key {rawNoteMIDI} not found or not supported, will be replaced by a pause.");
                            noteID = 0; //pause note
                        }
                        //raw notes must have default timing iirc
                        timing = secondsPerFullNote / _currentDefaultNoteLenght;
                        if (extendNextNote)
                        {
                            extendNextNote = false;
                            if (!ExtendLastNote(trackID, currentNoteIndex, noteID, timing))
                                return false;
                        }
                        else
                        {
                            SetNote(trackID, noteID, currentNoteIndex, timing);
                            currentNoteIndex++;
                        }
                        break;
                    case ' ':
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        break;
                    default:
                        lastValueIndex = OTHER_VALUE; //this value can't be extended by a dot
                        _unparsableNotesCount++;
                        Debug.LogError($"[Piano] MML parser doesn't understand the character '{currentChar}' and will ignore it.");
                        break;
                }
            }
            return true; //parsing this track succeeded without any errors
        }
        /// <summary>
        /// Extends the last note in the note array by the specified timing. Returns false if there was an error.
        /// </summary>
        /// <param name="trackID">ID of the track</param>
        /// <param name="currentNoteIndex">Index of the current extender note</param>
        /// <param name="currentNoteID">ID of the current extender note</param>
        /// <param name="timing">Timing of the extender note</param>
        /// <returns></returns>
        private bool ExtendLastNote(int trackID, int currentNoteIndex, int currentNoteID, float timing)
        {
            if (_debugParser)
                Debug.Log($"[Piano] This note was an extender note.");
            int lastNoteIndex = currentNoteIndex - 1;
            int lastNoteID = GetNote(trackID, lastNoteIndex);
            if (lastNoteIndex < 0)
            {
                _trackError = "[Piano] Error: An extender sign '&' must follow another note in a track.";
                return false;
            }
            else if (lastNoteID != currentNoteID)
            {
                _trackError = "[Piano] Error: An extender sign '&' must be between the same note letters in a track.";
                return false;
            }
            else
            {
                //add timing of the current note to the last note
                SetNote(trackID, lastNoteID, lastNoteIndex, timing + GetNoteTiming(trackID, lastNoteIndex));
                return true;
            }
        }
        /// <summary>
        /// Converts bpm to time in seconds per full note
        /// </summary>
        private float ConvertToSecondsPerNote(float bpm)
        {
            return (1f / (bpm / 60f)) * 4f;
        }
        /// <summary>
        /// Converts a midi note to a key ID (relatively expensive search function, limit usage).
        /// Rerturn -1 if the key was not / is not supported
        /// </summary>
        private int ConvertMidiNoteToKeyID(int midiNote)
        {
            //check if we have a corresponding white key
            int index = System.Array.IndexOf(_whiteKeyToMidi, midiNote);
            if (index < 0)
            {
                //check if we have a corresponding black key
                index = System.Array.IndexOf(_blackKeyToMidi, midiNote);
                if (index < 0)
                {
                    return -1; //key not found or not supported
                }
                else
                {
                    //return the corresponding black key
                    return index + WHITE_KEYS_LENGTH;
                }
            }
            else
            {
                //return the corresponding white key
                return index;
            }
        }
        /// <summary>
        /// Extract the integer number that follows after index in a char array.
        /// Returns -1 if no integer was found.
        /// </summary>
        private int GetNextInteger(int index, char[] trackChars)
        {
            index++;
            bool isNumeric = false;
            string numberString = "";
            while (index < trackChars.Length)
            {
                if (!_ALL_NUMBERS.Contains(trackChars[index].ToString()))
                    break;
                numberString += trackChars[index];
                isNumeric = true;
                index++;
            }
            if (!isNumeric)
                return -1;
            else
                return int.Parse(numberString);
        }
        /// <summary>
        /// Counts all notes in a track
        /// </summary>
        private int GetNoteCount(char[] trackChars)
        {
            int noteCount = 0;
            //count how many notes the track has in total
            for (int i = 0; i < trackChars.Length; i++)
            {
                switch (trackChars[i])
                {
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'r':
                    case 'n':
                        noteCount++;
                        break;
                    case '&':
                        noteCount--;
                        break;
                }
            }
            return noteCount;
        }
        /// <summary>
        /// Returns wheter or not an integer represents a valid note length
        /// </summary>
        private bool CheckNoteLenght(float noteLenght)
        {
            switch (noteLenght)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                case 16:
                case 32:
                case 64:
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Converts a regular note symbol to the sharp note representation (as a sharp note)
        /// </summary>
        private string ConvertToSharpNote(char noteName)
        {
            switch (noteName)
            {
                case 'a':
                    return "a#";
                case 'b':
                    return "c";
                case 'c':
                    return "c#";
                case 'd':
                    return "d#";
                case 'e':
                    return "f";
                case 'f':
                    return "f#";
                case 'g':
                    return "g#";
                default:
                    _unparsableNotesCount++;
                    Debug.LogError($"[Piano] Tried to convert '{noteName}' to a sharp note but it's not a note, setting a pause note instead.");
                    return "r"; //returning a pause note instead
            }
        }
        /// <summary>
        /// Converts a regular note symbol to the flat note representation (as a sharp note)
        /// </summary>
        private string ConvertToFlatNote(char noteName)
        {
            switch (noteName)
            {
                case 'a':
                    return "g#";
                case 'b':
                    return "a#";
                case 'c':
                    return "b";
                case 'd':
                    return "c#";
                case 'e':
                    return "d#";
                case 'f':
                    return "e";
                case 'g':
                    return "f#";
                default:
                    _unparsableNotesCount++;
                    Debug.LogError($"[Piano] Tried to convert '{noteName}' to a flat note but it's not a note, setting a pause note instead.");
                    return "r"; //returning a pause note instead
            }
        }
        /// <summary>
        /// Converts an octave 1-7 to our own index, returns -1 if it's invalid
        /// </summary>
        private int ConvertToOctaveID(int rawOctave)
        {
            //don't allow octaves that are clearly out of range
            if (rawOctave < 0 || rawOctave > 8)
                return -1;
            if (rawOctave == 0)
            {
                Debug.LogError("[Piano] Octave 0 is unsupported on this piano so we set octave 1 instead (offset is still stored).");
                rawOctave = 1;
            }
            else if (rawOctave == 8)
            {
                Debug.LogError("[Piano] Octave 8 is unsupported on this piano so we set octave 7 instead (offset is still stored).");
                rawOctave = 7;
            }
            rawOctave--; //to get a zero-indexed octave
                         //our octaves are reversed, so that 7 (6) is 1 (0) and 1 (0) is 7 (6)
            rawOctave = 6 - rawOctave;
            if (rawOctave >= 0 && rawOctave < 7)
                return rawOctave;
            else
                return -1;
        }
        /// <summary>
        /// Takes in an offset from C from the current octave and returns the corresponding note index.
        /// Key is shifted 
        /// </summary>
        private int ConvertToNoteID(int octaveID, string noteName)
        {
            int boneIndex;
            bool isBlack = false;
            //black note
            switch (noteName)
            {
                case "c":
                    boneIndex = 6;
                    break;
                case "c#":
                    boneIndex = 4;
                    isBlack = true;
                    break;
                case "d":
                    boneIndex = 5;
                    break;
                case "d#":
                    boneIndex = 3;
                    isBlack = true;
                    break;
                case "e":
                    boneIndex = 4;
                    break;
                case "f":
                    boneIndex = 3;
                    break;
                case "f#":
                    boneIndex = 2;
                    isBlack = true;
                    break;
                case "g":
                    boneIndex = 2;
                    break;
                case "g#":
                    boneIndex = 1;
                    isBlack = true;
                    break;
                case "a":
                    boneIndex = 1;
                    break;
                case "a#":
                    boneIndex = 0;
                    isBlack = true;
                    break;
                case "b":
                    boneIndex = 0;
                    break;
                case "p":
                case "r":
                case "-":
                    return 0;
                default:
                    Debug.LogError($"[Piano] Tried to interpret note '{noteName}', but only the notes C,C#,D,D#,E,F,F#,G,G#,A,A#,B or p/r/- for a pause is allowed.");
                    return -1;
            }
            if (isBlack)
            {
                //one octave has 5 black keys
                boneIndex += (5 * octaveID) + WHITE_KEYS_LENGTH;
#if BONE_DEBUG
                Debug.Log($"[Piano] Note '{noteName}' is black and boneIndex is {boneIndex} in octaveIndex {octaveID}");
#endif
            }
            else
            {
                //one octave has 7 white keys, our piano has a key on index 0 which is ignored by +1
                boneIndex += 1 + (7 * octaveID);
#if BONE_DEBUG
                Debug.Log($"[Piano] Note '{noteName}' is white and boneIndex is {boneIndex} in octaveIndex {octaveID}");
#endif
            }
            if (boneIndex >= 1 && boneIndex < ALL_KEYS_LENGHT - 1)
                return boneIndex;
            else
            {
                Debug.LogError("[Piano] (Internal script error) Track bone index is out of bounds, please report this error to us.");
                return -1;
            }

        }
        #endregion MMLParser
        #region VFParser
        /// <summary>
        /// Each field is split by a |
        /// Field 1: Beats per minute of the track
        /// Field 2: Otave offset from left side of piano, 1-7 possible)
        /// Field 3: Speed of the notes in meters per minute, 1-999 possible
        /// Octaves can change during the song, simply add another octave field in between.
        /// A note field can contain one or multiple notes, seperated by a semikolon ;
        /// A note must start with the note type (1 for a full note, 2 for a half, 4 for a 1/4 note, 8 for a 1/8 note)
        /// followed by the note itself C-D-E-F-G
        /// Pause (whole note): 1-
        /// Sharp note start with #
        /// Multiple notes at once: Split them with ;
        /// Changes in timing signature: e.g. T44 for 4/4, T34 for 3/4, and T68 for 6/8 (counts for the following notes)
        /// Example: "VF%120bpm|%3oct|%10mm|1-|1E|1E|1E|1-|1E|1E|1E|1-|1E|1G|1C|1D|1E|1-|1-|1-|1-|1F|1F|1F|1F|1F|1F|1E|1E|4E|4E|1E|1D|1D|1E|1D|4-|1G|4-";
        /// Template: https://docs.google.com/spreadsheets/d/15D0Oh7OVx4HGGxjMzgRWXYUgwxowMc6vDujUfCdm3iQ/edit#gid=0
        /// </summary>
        private bool ParseVFFormat(string track)
        {
            string[] fields = track.Split('|');
            if (fields.Length < 4)
            {
                _trackError = "Not enough fields found. Are you using the field '|' seperator?";
                return false;
            }
            string field = fields[0];
            _currentBPM = ReadIntegerFromField(field, "bpm");
            if (_currentBPM == -1)
            {
                _trackError = $"First field with content '{field}' is not a bpm field. Usage: VF%120bpm|%3oct|%10mm|...";
                return false;
            }
            else if (_currentBPM == -2)
            {
                _trackError = $"bpm field (first field) with content '{field}' does not contain a leading integer number. Usage: VF%120bpm|%3oct|%10mm|...";
                return false;
            }
            float secondsPerBeat = 60f / (float)_currentBPM;
            Debug.Log($"[Piano] Track has {_currentBPM} bpm which is {secondsPerBeat} seconds per full note.");
            field = fields[1];
            int speed = ReadIntegerFromField(field, "mm");
            if (speed == -1)
            {
                _trackError = $"Second field with content '{field}' is not a speed field. Usage: VF%120bpm|%3oct|%10mm|...";
                return false;
            }
            else if (speed == -2)
            {
                _trackError = $"Speed field (second field) with content '{field}' does not contain a leading integer number.";
                return false;
            }
            else if (speed < 1 || speed > 999)
            {
                _trackError = $"Speed field (third field) with content '{field}' is out of range (only speed 1-999 is possible)";
                return false;
            }
            _blockSpeedMs = (float)speed / 60f;
            field = fields[2];
            int oct = ReadIntegerFromField(field, "oct");
            if (oct == -1)
            {
                _trackError = $"Third field with content '{field}' is not an octave field. Usage: VF%120bpm|%3oct|%10mm|...";
                return false;
            }
            else if (oct == -2)
            {
                _trackError = $"Octave field (third field) with content '{field}' does not contain a leading integer number.";
                return false;
            }
            else if (oct < 1 || oct > 7)
            {
                _trackError = $"Octave field (third field) with content '{field}' is out of range (only octave 1-7 is possible)";
                return false;
            }
            oct--; //to get a zero-indexed octave
                   //our octaves are reversed, so that 7 (6) is 1 (0) and 1 (0) is 7 (6)
            oct = 6 - oct;
            Debug.Log($"[Piano] (internal) first octave was translated to index {oct}");
            _unparsableNotesCount = 0;
            ResetAllTracks();
            SetupNoteArray(trackID: 0, length: fields.Length - 3);
            Debug.Log($"[Piano] Track has {fields.Length - 3} fields which are now being parsed");
            int currentTrackNoteIntex = 0;
            for (int i = 3; i < fields.Length; i++)
            {
                field = fields[i].Trim();
                if (field.Length == 0)
                {
                    Debug.LogError($"Will skip field {i} because it's empty.");
                    _unparsableNotesCount++;
                }
                else if (field.Substring(0, 1) == "%") //check if it's a special field
                {
                    //allow changes in octave during the song
                    oct = ReadIntegerFromField(field, "oct");
                    if (oct == -1)
                    {
                        _trackError = $"Special field {i} in track is not an octave field. You can only change the octave during the song.";
                        return false;
                    }
                    else if (oct == -2)
                    {
                        _trackError = $"Octave field {i} in track does not contain a leading integer number.";
                        return false;
                    }
                    else if (oct < 1 || oct > 7)
                    {
                        _trackError = $"Octave field {i} in track is out of range (only octave 1-7 is possible)";
                        return false;
                    }
                    oct--; //to get a zero-indexed octave
                           //our octaves are reversed, so that 7 (6) is 1 (0) and 1 (0) is 7 (6)
                    oct = 6 - oct;
                }
                else
                {
                    string[] notesAtOnce = field.Split(';');
                    string noteStr;
                    for (int n = 0; n < notesAtOnce.Length; n++)
                    {
                        noteStr = notesAtOnce[n].Trim();
                        if (noteStr.Length >= 2)
                        {
                            float timing = ConvertToTiming(noteStr.Substring(0, 1), secondsPerBeat);
                            if (timing == -1)
                            {
                                Debug.LogError($"[Piano] Could not parse note length '{noteStr.Substring(0, 1)}' in field {i} from track with field content {field}");
                                break;
                            }
                            int noteIndex = ConvertToNoteID(oct, noteStr.Substring(1));
                            if (noteIndex == -1)
                            {
                                Debug.LogError($"[Piano] Could not parse note '{noteStr.Substring(1)}' in field {i} from track with field content {field}");
                                break;
                            }
                            _track0Notes[currentTrackNoteIntex] = noteIndex;
                            _track0NotesTiming[currentTrackNoteIntex] = timing;
                            currentTrackNoteIntex++;
                        }
                        else
                        {
                            Debug.LogError($"[Piano] Will not parse field {i} from track with content {field} (under 2 characters)");
                            _unparsableNotesCount++;
                        }
                    }
                }
            }
            Debug.Log($"[Piano] Parsing finished. Track had {currentTrackNoteIntex} notes and {_unparsableNotesCount} unparseable fields.");
            return true;
        }
        private float ConvertToTiming(string noteField, float secondsPerBeat)
        {
            switch (noteField)
            {
                case "1":
                case "2":
                case "4":
                case "8":
                    return secondsPerBeat / int.Parse(noteField);
                default:
                    Debug.LogError($"[Piano] Tried to read note timing from '{noteField}', but only the character 1,2,4 or 8 is allowed.");
                    return -1;
            }
        }
        /// <summary>
        /// Reads the integer followed by the identifier
        /// </summary>
        /// <param name="input">The fields input</param>
        /// <param name="identifier">String part that needs to be in the input, else it returns -1</param>
        /// <returns></returns>
        private int ReadIntegerFromField(string input, string identifier)
        {
            bool isNumeric = false;
            string numberString = "";
            if (input.Length < 2)
            {
                Debug.LogError($"[Piano] Special field content '{input}' needs to have at least 2 characters.");
                return -1; //for "identifier not found"
            }
            else if (input.Substring(0, 1) != "%")
            {
                Debug.LogError($"[Piano] Special field content '{input}' needs to start with '$'.");
                return -1; //for "identifier not found"
            }
            else if (!input.Contains(identifier))
            {
                Debug.LogError($"[Piano] Special field content '{input}' needs to contain the identifier '{identifier}'.");
                return -1; //for "identifier not found"
            }
            else
            {
                char identifierFirstChar = identifier.ToCharArray()[0];
                char[] content = input.ToCharArray();
                int i = 1; //skip first char which is just %
                while (content[i] != identifierFirstChar)
                {
                    if (!_ALL_NUMBERS.Contains(content[i].ToString()))
                    {
                        Debug.LogError($"[Piano] Special field content '{input}' has the non-numeric character '{content[i]}' in front of the identifier.");
                        isNumeric = false;
                        break;
                    }
                    numberString += content[i];
                    isNumeric = true;
                    i++;
                }
                if (!isNumeric)
                {
                    Debug.LogError($"[Piano] Special field content '{input}' has no numeric character in front of the identifier.");
                    return -2;
                }
                else
                {
                    return int.Parse(numberString);
                }
            }
        }
        #endregion VFParser
        #region StartSetup
        /// <summary>
        /// Setting up this class at game start
        /// </summary>
        private void Start()
        {
            //this is an early attempt to support different piano scales. Use with caution for now.
            //by allowing the scale to only change _once_ during Start(), we don't need to add extra constants
            //and thus reduce the heap size. This means however that we can't dynamically adjust to each avatar,
            //but that is a bad concept anyway for a social experience because it would look weird on remote clients
            float pianoScale = this.transform.lossyScale.x;
            KEY_THICKNESS_WHITE *= pianoScale;
            KEY_THICKNESS_BLACK *= pianoScale;
            BLACK_KEY_SIZE *= pianoScale;
            DEFAULT_BLOCK_SPEED_MS *= pianoScale;
            NOTE_GAP_DIVIDER *= pianoScale;
            DEFAULT_INTERACTION_DISTANCE *= pianoScale;
            _whiteGameNoteBlueprint.transform.localScale *= pianoScale;
            _blackGameNoteBlueprint.transform.localScale *= pianoScale;
            VERTICAL_BLOCK_SIZE *= pianoScale;
            _blockSpeedMs = DEFAULT_BLOCK_SPEED_MS;
            _interactionDistance = DEFAULT_INTERACTION_DISTANCE;
            _fingerThickness = FINGER_THICKNESS_DEFAULT;
            _headThickness = HEAD_THICKNESS_DEFAULT;
            _footThickness = FOOT_THICKNESS_DEFAULT;
            _audioSpawnTimes = new float[MAX_AUDIO_SOURCES];
            _audioSourcesSpawned = new AudioSource[MAX_AUDIO_SOURCES];
            if (_menuDisplayObj != null)
                _menuDisplayObj.SetActive(false);
            if (_gameStatsDisplay != null)
                _gameStatsDisplay.SetActive(false);
            if (_customSongMenu != null)
                _customSongMenu.SetActive(false);
            //No VRChat local player in this port. Hide the editor test hand if one was assigned.
            if (_fakeHand != null)
                _fakeHand.gameObject.SetActive(false);
            PLAY_TIME += 0.05f; //adding a slight delay to make sure we don't destroy the audio source too early
            _hasFinishedStart = true;
        }
        /// <summary>
        /// The real setup function which is called when player is first in range
        /// </summary>
        private void SetupPiano()
        {
            //free all fingers
            for (int i = 0; i < _currentFingerKeyBinding.Length; i++) { _currentFingerKeyBinding[i] = -1; };
            _menuToggleHeight = _menuToggleText.transform.position.y;
            _menuHeight = _menuNetworkmodeText.transform.position.y;
            //Get bounds for the keys
            _allBlackKeysBounds = _allBlackKeysBoxCollider.bounds;
            _allWhiteKeysBounds = _allWhiteKeysBoxCollider.bounds;
            _menuToggleArea = _menuToggleBoxCollider.bounds;
            _menuArea = _menuBoxCollider.bounds;
            //determine the size of one white key
            _whiteKeySize = _allWhiteKeysBounds.size.x / WHITE_KEYS_LENGTH;
            //Set key projection for angle calculations
            _keyProjectionForward = -_keyRotationZeroRef.forward.normalized;
            //set all keys into default state and layout
            SetupKeys();
            //confirm that setup completed
            _isSetup = true;
            _receiveNetworkEvents = true;
            Debug.Log("[Piano] is setup.");
        }
        /// <summary>
        /// initializing all key arrays with the default values
        /// </summary>
        private void SetupKeys()
        {
            _wasInBound = new bool[ALL_KEYS_LENGHT];
            _wasTriggered = new bool[ALL_KEYS_LENGHT];
            _lastTriggerTime = new float[ALL_KEYS_LENGHT];
            _currentKeyAngle = new float[ALL_KEYS_LENGHT];
            _isHeld = new bool[ALL_KEYS_LENGHT];
            InitializeBlackKeyMapping();
        }
        /// <summary>
        /// Time when the last "exceeded audio limit" error message was printed
        /// </summary>
        private float _exceededLimitPrintedLastTime;
        /// <summary>
        /// Sets up at which position each black key is in an array
        /// </summary>
        private void InitializeBlackKeyMapping()
        {
            _blackKeyMapping = new int[] { -1, -1, 0, 1, 2, -1, 3, 4, -1, 5, 6, 7, -1, 8, 9, -1, 10, 11, 12, -1, 13, 14, -1, 15, 16, 17, -1, 18, 19, -1, 20, 21, 22, -1, 23, 24, -1, 25, 26, 27, -1, 28, 29, -1, 30, 31, 32, -1, 33, 34, -1, 35, -1 };
            if (_blackKeyMapping.Length != WHITE_KEYS_LENGTH + 1)
                Debug.LogError("[Piano] _blackKeyMapping has an unexpected length");
            _blackKeyToMidi = new int[] { 106, 104, 102, 99, 97, 94, 92, 90, 87, 85, 82, 80, 78, 75, 73, 70, 68, 66, 63, 61, 58, 56, 54, 51, 49, 46, 44, 42, 39, 37, 34, 32, 30, 27, 25, 22 };
            if (_blackKeyToMidi.Length != BLACK_KEYS_LENGTH)
                Debug.LogError("[Piano] _blackKeyToMidi has an unexpected length");
            _whiteKeyToMidi = new int[] { 108, 107, 105, 103, 101, 100, 98, 96, 95, 93, 91, 89, 88, 86, 84, 83, 81, 79, 77, 76, 74, 72, 71, 69, 67, 65, 64, 62, 60, 59, 57, 55, 53, 52, 50, 48, 47, 45, 43, 41, 40, 38, 36, 35, 33, 31, 29, 28, 26, 24, 23, 21 };
            if (_whiteKeyToMidi.Length != WHITE_KEYS_LENGTH)
                Debug.LogError("[Piano] _whiteKeyToMidi has an unexpected length");
        }
        #endregion StartSetup
        #region Update
        /// <summary>
        /// Only run script when target LOD is active
        /// </summary>
        private void Update()
        {
            //first clean up audioSources
            if (_amountOfSpawnedAudioSources > 0)
            {
                CleanAudioSources();
            }

            //In this single-player port the "player" is the FPS camera/player root.
            //Fall back to the editor test hand when no camera is present.
            Vector3 playerPosition;
            if (LocalPlayerProxy.IsValid)
                playerPosition = LocalPlayerProxy.Position;
            else if (_fakeHand != null)
                playerPosition = _fakeHand.position;
            else
                return;

            float distanceToPlayer = Vector3.Distance(playerPosition, this.transform.position);
            if (distanceToPlayer < MAX_AUDIO_DISTANCE)
            {
                //first setup is done when we are actually in range
                if (!_isSetup)
                {
                    if (Time.timeSinceLevelLoad > 7f)
                    {
                        SetupPiano();
                        //every user should start in watch mode
                        //set game mode to watch mode
                        SwitchToGameMode(GAME_MODE_WATCH);
                    }
                    else
                        return;
                }
                //allow piano keys to be pushed
                if (distanceToPlayer < _interactionDistance)
                {
                    CheckAllFingers();
                }
                if (_runLevel)
                    RunLevel();
                if (_moveBlocks)
                    MoveActiveBlocks();
                //run piano keys
#if TEST_DEBUG
                _isHeldDebugCount = 0;
                _wasInBoundDebugCount = 0;
                _wasTriggeredDebugCount = 0;
#endif
                RunAllKeys();
#if TEST_DEBUG
                if (_debugFrameCount < 90)
                    _debugFrameCount++;
                else
                {
                    Debug.Log($"_isHeld: {_isHeldDebugCount}, _wasInBound: {_wasInBoundDebugCount}, _wasTriggered: {_wasTriggeredDebugCount}, _amountOfSpawnedAudioSources: {_amountOfSpawnedAudioSources}, _runLevel: {_runLevel}, _moveBlocks: {_moveBlocks}, _isHeldCount: {_isHeldCount}, _wasInBouncCount: {_wasInBoundCount}");
                    _debugFrameCount = 0;
                }
#endif
            }
        }
        #endregion Update
        #region Audio
        /// <summary>
        /// translates a midi key into the equivaltent pitch needed to be applied to a C4 sound where C4 == 440hz
        /// </summary>
        private float KeyToPitch(int midiKey)
        {
            int c4Key = midiKey - 60;
            float pitch = Mathf.Pow(2, c4Key / 12f);
            return pitch;
        }
        /// <summary>
        /// Cleans up old audio sources that finished playing
        /// </summary>
        private void CleanAudioSources()
        {
#if UDON_VOLUME_CURVES
            //reduce volume for all audio sources
            for (int i = 0; i < _amountOfSpawnedAudioSources; i++)
            {
                AudioSource audioSrc = _audioSourcesSpawned[i];
                if (audioSrc != null)
                    audioSrc.volume -= Time.deltaTime / PLAY_TIME;
                else
                    Debug.LogError("[Piano] AudioSource was null, can't reduce volume");
            }
#endif
            //destroy only the first audio sources that exceeded their playtime to ensure no frametime peaks occur
            //oldest one is always the lowest in the stack, check if that one is still playing
            if (Time.timeSinceLevelLoad - _audioSpawnTimes[0] <= PLAY_TIME)
                return;
            //destroy AudioSource & gameObject it is on
            GameObject objToDestroy = _audioSourcesSpawned[0].gameObject;
            if (objToDestroy != null)
            {
                Destroy(objToDestroy);
            }
            else
                Debug.LogError("[Piano] AudioSource-GameObject was null, can't destroy it");
                //push stack one down
            System.Array.Copy(_audioSpawnTimes, 1, _audioSpawnTimes, 0, _amountOfSpawnedAudioSources - 1);
            System.Array.Copy(_audioSourcesSpawned, 1, _audioSourcesSpawned, 0, _amountOfSpawnedAudioSources - 1);
            _amountOfSpawnedAudioSources--;
            //This is what we did instead before we had access to the static array functions in Udon, 
            //it does the same, but is way less performant:
            /*
            for (int i = 1; i < _amountOfSpawnedAudioSources; i++)
            {
                _audioSpawnTimes[i - 1] = _audioSpawnTimes[i];
                _audioSourcesSpawned.SetValue(_audioSourcesSpawned.GetValue(i), i - 1);
            }
            */
        }
        #endregion Audio
        #region RunLevel
        /// <summary>
        /// Game score variables
        /// </summary>
        private float _totalCorrectHitTime;
        private float _totalMissedHitTime;
        private int _totalGoodHits;
        private int _totalWrongHits;
        private int _totalMissedHits;
        private bool[] _gameKeyShouldBePressedRightNow = new bool[88];
        /// <summary>
        /// Amount of all blocks that spawned since the start of this level
        /// </summary>
        private int _totalSpawnedBlocks;
        /// <summary>
        /// Whether or not its a preview of the game. If true, notes will play automaticly.
        /// </summary>
        private bool _gameLevelPreview;
        /// <summary>
        /// Is called when a level is submitted
        /// </summary>
        public void _OnEndEditLevel()
        {
            string input = _levelInputField.text.Trim();
            ProcessLevelString(input);
            if (_pendingLevel)
            {
                _keyStopsSong = true;
                _songLabelBlueprint.SetActive(true); //this gameobject is also the "STOP" label
                _pendingLevel = false;
                InitializeGameLevel();
            }
        }
        /// <summary>
        /// Processes a level string (either MML or VF) and starts the level if that's successful
        /// </summary>
        private void ProcessLevelString(string input)
        {
            if (input.Length < 2)
                return;
            bool parseResult;
            if (input.Substring(0, 2) == "VF")
                parseResult = ParseVFFormat(input);
            else
                parseResult = ParseMML(input);
            if (!parseResult)
            {
                _levelErrorText.text = $"<color=red>{_trackError}</color>";
                Debug.LogError($"[Piano] {_trackError}");
                Debug.LogError($"[Piano] Level (bad) input was '{input}'");
            }
            else
            {
                _pendingLevel = true;
                Debug.Log("[Piano] Parsed input successfully. Starting song...");
                _levelErrorText.text = "<color=green>Parsed track successfully.\nPress the rightmost white key to start the level.</color>";
            }
        }
        /// <summary>
        /// Is called during update if _runLevel is true.
        /// </summary>
        private void RunLevel()
        {
            if (_hasMelody)
                RunTrack(0, _timeNextTrack0Note, _runningTrack0NoteIndex);
            if (_hasHarmony1)
                RunTrack(1, _timeNextTrack1Note, _runningTrack1NoteIndex);
            if (_hasHarmony2)
                RunTrack(2, _timeNextTrack2Note, _runningTrack2NoteIndex);
            if (_hasSong)
                RunTrack(3, _timeNextTrack3Note, _runningTrack3NoteIndex);
            ResfreshGameStats();
        }
        /// <summary>
        /// After parsing and before the level starts playing the first note, this function needs to get called
        /// </summary>
        private void InitializeGameLevel()
        {
            _runningTrack0NoteIndex = 0;
            _runningTrack1NoteIndex = 0;
            _runningTrack2NoteIndex = 0;
            _runningTrack3NoteIndex = 0;
            _timeNextTrack0Note = Time.time;
            _timeNextTrack1Note = Time.time;
            _timeNextTrack2Note = Time.time;
            _timeNextTrack3Note = Time.time;
            _runLevel = true;
            if (!_gameLevelPreview)
            {
                _moveBlocks = true;
                InitializeBlocks();
            }
        }
        /// <summary>
        /// Runs a single track and returns the new index
        /// </summary>
        /// <param name="trackNumber">Number of this track</param>
        /// <param name="runningTrack0NoteIndex">Current index for this track</param>
        /// <param name="track0Notes">Note array for this track</param>
        /// <param name="track0NotesTiming">Timer array for this track</param>
        /// <returns></returns>
        private void RunTrack(int trackID, float nextTimer, int runningTrackNoteIndex)
        {
            while (Time.time - nextTimer >= 0)
            {
                if (runningTrackNoteIndex >= GetNoteArrayLenght(trackID))
                {
                    //end the current track
                    switch (trackID)
                    {
                        case 0:
                            _hasMelody = false;
                            break;
                        case 1:
                            _hasHarmony1 = false;
                            break;
                        case 2:
                            _hasHarmony2 = false;
                            break;
                        case 3:
                            _hasSong = false;
                            break;
                    }
                    //potentially end the level here when all tracks have ended
                    if (!_hasMelody && !_hasHarmony1 && !_hasHarmony2 && !_hasSong)
                    {
                        _runLevel = false;
                    }
                    return;
                }
                float noteDuration = GetNoteTiming(trackID, runningTrackNoteIndex);
                //play the current note
                int key = GetNote(trackID, runningTrackNoteIndex);
                if (key > 0) //zero-key is a pause, negative keys are hold-keys
                {
                    if (_gameLevelPreview)
                    {
                        if (!_isHeld[key])
                        {
                            _isHeld[key] = true;
                            _isHeldCount++;
                        }
                        _currentKeyAngle[key] = _KEY_MAX_ROTATION;
                    }
                    else
                    {
                        SpawnBlock(noteDuration, key);
                    }
                }
                //increase the timer by the time of the current note
                nextTimer += noteDuration;
                //check the next note
                runningTrackNoteIndex++;
            }
            //store the new timer value and note index in the right field
            switch (trackID)
            {
                case 0:
                    _runningTrack0NoteIndex = runningTrackNoteIndex;
                    _timeNextTrack0Note = nextTimer;
                    break;
                case 1:
                    _runningTrack1NoteIndex = runningTrackNoteIndex;
                    _timeNextTrack1Note = nextTimer;
                    break;
                case 2:
                    _runningTrack2NoteIndex = runningTrackNoteIndex;
                    _timeNextTrack2Note = nextTimer;
                    break;
                case 3:
                    _runningTrack3NoteIndex = runningTrackNoteIndex;
                    _timeNextTrack3Note = nextTimer;
                    break;
            }
        }
        /// <summary>
        /// A collection of currently rendered note blocks
        /// </summary>
        private Transform[] _activeBlocks = new Transform[MAX_BLOCKS_AMOUNT];
        private float[] _blockHitDuration = new float[MAX_BLOCKS_AMOUNT];
        private float[] _blockTimer = new float[MAX_BLOCKS_AMOUNT];
        private bool[] _blockWasHit = new bool[MAX_BLOCKS_AMOUNT];
        private int[] _blockKey = new int[MAX_BLOCKS_AMOUNT];
        private int[] _blockState = new int[MAX_BLOCKS_AMOUNT];
        private int _amountOfSpawnedBlocks = 0;
        ///All block states
        private const int BLACK_MOVING_TO_HIT = 1;
        private const int WHITE_MOVING_TO_HIT = 2;
        private const int BLACK_MOVING_TO_END_HIT = 3;
        private const int WHITE_MOVING_TO_END_HIT = 4;
        private const int BLACK_MOVING_TO_DESTROY = 5;
        private const int WHITE_MOVING_TO_DESTROY = 6;
        /// Is called when there is currently a game level running.
        /// Points are rewarded for the whole time at which a key is pressed while the note is hitting it, 
        /// minus the time a key is pressed while the not is not hitting it.
        /// Similar, there is a counter for each correct note being hit, minus the incorrect ones.
        private Vector3 _blockSpawnPos;
        private float _whiteBlockHitTravelDistance;
        private float _blackBlockHitTravelDistance;
        /// <summary>
        /// Spawns a new block at the correct spawn height and size
        /// </summary>
        private void SpawnBlock(float timeToNextBlock, int key)
        {
            if (_amountOfSpawnedBlocks < MAX_BLOCKS_AMOUNT - 1)
            {
                GameObject blueprint;
                int nextState;
                float hitTravelDistance;
                float spawnPosX;
                if (key < WHITE_KEYS_LENGTH)
                {
                    //it's a white key
                    hitTravelDistance = _whiteBlockHitTravelDistance;
                    nextState = WHITE_MOVING_TO_HIT;
                    blueprint = _whiteGameNoteBlueprint;
                    spawnPosX = _whiteKeysRoot.GetChild(key).position.x;
                }
                else
                {
                    //it's a black key
                    hitTravelDistance = _blackBlockHitTravelDistance;
                    nextState = BLACK_MOVING_TO_HIT;
                    blueprint = _blackGameNoteBlueprint;
                    spawnPosX = _blackKeysRoot.GetChild(key - WHITE_KEYS_LENGTH).position.x;
                }
                Transform newBlock = Instantiate(blueprint).transform;
                newBlock.gameObject.SetActive(true);
                _totalSpawnedBlocks++;
                //given the current travel speed, calculate how long the block would need to be. Scale 1 : 1m height
                float neededScale = timeToNextBlock * NOTE_GAP_DIVIDER / (VERTICAL_BLOCK_SIZE / _blockSpeedMs); // reducing to xx% would give the user a chance to move the finger to the next note
                _blockHitDuration[_amountOfSpawnedBlocks] = timeToNextBlock * NOTE_GAP_DIVIDER;
                _blockState[_amountOfSpawnedBlocks] = nextState;
                _blockTimer[_amountOfSpawnedBlocks] = (hitTravelDistance / _blockSpeedMs) + Time.time; //set time at which block will hit the surface
                _activeBlocks[_amountOfSpawnedBlocks] = newBlock;
                _blockWasHit[_amountOfSpawnedBlocks] = false;
                _blockKey[_amountOfSpawnedBlocks] = key;
                _amountOfSpawnedBlocks++;
                newBlock.localScale = new Vector3(newBlock.localScale.x, neededScale, newBlock.localScale.z);
                //calculate at which position the note should spawn and move the new note there
                _blockSpawnPos.x = spawnPosX;
                newBlock.SetParent(_blockSpawnParent, worldPositionStays: true);
                newBlock.position = _blockSpawnPos;
            }
            else if (Time.timeSinceLevelLoad - _exceededLimitPrintedLastTime > 3f)
            {
                Debug.LogError("[Piano] Exceeded maximum amount of blocks that can spawn (this error is only printed each 3 seconds)");
                _exceededLimitPrintedLastTime = Time.timeSinceLevelLoad;
                return;
            }
        }
        /// <summary>
        /// Functions needs to get called before a game starts.
        /// Sets up all variables and makes sure that existing blocks are destroyed.
        /// </summary>
        private void InitializeBlocks()
        {
            for (int i = 0; i < _gameKeyShouldBePressedRightNow.Length; i++) { _gameKeyShouldBePressedRightNow[i] = false; }
            //save where blocks should spawn
            _blockSpawnPos = _blockSpawnHeightRef.position;
            //reset block spawn parent
            _blockSpawnParent.position = _blockSpawnPos;
            //reset score variables
            _totalSpawnedBlocks = 0;
            _totalGoodHits = 0;
            UpdateCorrectHitsUI();
            _totalWrongHits = 0;
            UpdateWrongHitsUI();
            _totalMissedHits = 0;
            UpdateMissedHitsUI();
            _totalCorrectHitTime = 0;
            _totalMissedHitTime = 0;
            //measure how far each block can travel in meter until it hits the key surface
            _blackBlockHitTravelDistance = Mathf.Abs(Vector3.Distance(_blockSpawnHeightRef.position, _blackblockHitHeightRef.position));
            _whiteBlockHitTravelDistance = Mathf.Abs(Vector3.Distance(_blockSpawnHeightRef.position, _whiteblockHitHeightRef.position));
            //destroy all existing blocks if there are any
            for (int i = 0; i < _amountOfSpawnedBlocks; i++)
            {
                //destroy this block
                GameObject objToDestroy = _activeBlocks[i].gameObject;
                if (objToDestroy != null)
                    Destroy(objToDestroy);
                else
                    Debug.LogError("[Piano] Block-GameObject was invalid, can't destroy it");
            }
            //reset counter
            _amountOfSpawnedBlocks = 0;
            //make sure there are no blocks left, else print an error out
            if (_blockSpawnParent.childCount > 5)
            {
                int childCound = _blockSpawnParent.childCount;
                Debug.LogError($"[Piano] Found {childCound} blocks that were not cleaned up correctly.");
                return;
            }
        }
        /// <summary>
        /// Is called when a key gets pressed during the game
        /// </summary>
        private void KeyPressDuringGame(int key)
        {
            if (_keyStopsSong)
            {
                if (key == 0)
                {
                    _keySelectsSong = true;
                    _keyStopsSong = false;
                    _songLabelBlueprint.SetActive(false); //this gameobject is also the "STOP" label
                    ToggleKeySongLabels(true);
                    //key 0 stops the current level
                    //reset all tracks and blocks
                    ResetAllTracks();
                    InitializeBlocks();
                    //stop moving blocks
                    _moveBlocks = false;
                    SendSongSelection(0);
                    if (_currentGameMode == GAME_MODE_PLAY)
                        SetGameUIheaders("Pianomania", "Press a key to start a level");
                    else if (_currentGameMode == GAME_MODE_PREVIEW)
                        SetGameUIheaders("Song Preview", "Press a key to start preview");
                    return;
                }
                else if (_gameKeyShouldBePressedRightNow[key] == false)
                {
                    _totalWrongHits += 1;
                    UpdateWrongHitsUI();
                }
            }
            else if (_keySelectsSong)
            {
                //key 0 does nothing here
                if (key == 0)
                {
                    return;
                }
                else if (key - 1 < _songMMLLibrary.Length && key > 0)
                {
                    int songID = key - 1;
                    //start a level from the library
                    ProcessLevelString(_songMMLLibrary[songID]);
                    //start the song if parsing was successful
                    if (_pendingLevel)
                    {
                        ToggleKeySongLabels(false);
                        _keySelectsSong = false;
                        _keyStopsSong = true;
                        _songLabelBlueprint.SetActive(true); //this gameobject is also the "STOP" label
                        _pendingLevel = false;
                        InitializeGameLevel();
                        SendSongSelection(songID);
                        if (_currentGameMode == GAME_MODE_PLAY)
                            SetGameUIheaders("Pianomania", "Press 'STOP' key to stop the level");
                        else if (_currentGameMode == GAME_MODE_PREVIEW)
                            SetGameUIheaders("Song Preview", "Press 'STOP' key to stop the preview");
                    }
                    else
                    {
                        //to show the error message
                        _customSongMenu.SetActive(true);
                    }
                }
            }
        }
        #endregion RunLevel
        #region GameMenu
        /// <summary>
        /// Switches between different game modes
        /// </summary>
        private void SwitchToGameMode(int gameMode)
        {
            _currentGameMode = gameMode;
            _keyStopsSong = false;
            _songLabelBlueprint.SetActive(false); //this gameobject is also the "STOP" label
            _forwardKeysToGameMode = false;
            ToggleKeySongLabels(false);
            //reset all tracks and blocks
            ResetAllTracks();
            InitializeBlocks();
            //set all to default off
            _keySelectsSong = false;
            _gameLevelPreview = false;
            _watchGame = false;
            _customSongMenu.SetActive(false);
            _gameStatsDisplay.SetActive(false);
            switch (gameMode)
            {
                case GAME_MODE_OFF:
                    SetGameUImode("<color=red>off</color>");
                    break;
                case GAME_MODE_PLAY:
                    if (!_songLibraryIsSetup)
                        SetupSongLibrary();
                    SetGameUIheaders("Pianomania", "Press a key to start a level");
                    SetGameUImode("<color=green>play</color>");
                    ToggleKeySongLabels(true);
                    _keySelectsSong = true;
                    _forwardKeysToGameMode = true;
                    _gameStatsDisplay.SetActive(true);
                    break;
                case GAME_MODE_PREVIEW:
                    SetGameUIheaders("Song Preview", "Press a key to start preview");
                    SetGameUImode("<color=green>preview</color>");
                    ToggleKeySongLabels(true);
                    _forwardKeysToGameMode = true;
                    _keySelectsSong = true;
                    _gameLevelPreview = true;
                    break;
                case GAME_MODE_CUSTOM_SONG:
                    _forwardKeysToGameMode = true;
                    SetGameUIheaders("Pianomania", "Enter custom MML to start");
                    SetGameUImode("<color=green>custom</color>");
                    _gameStatsDisplay.SetActive(true);
                    _customSongMenu.SetActive(true);
                    break;
                case GAME_MODE_CUSTOM_SONG_PREVIEW:
                    _forwardKeysToGameMode = true;
                    SetGameUIheaders("Song Preview", "Enter custom MML to start");
                    SetGameUImode("<color=green>custom preview</color>");
                    _customSongMenu.SetActive(true);
                    _gameLevelPreview = true;
                    break;
                case GAME_MODE_WATCH:
                    SetGameUImode("<color=green>watch</color>");
                    _watchGame = true;
                    break;
            }
        }
        /// <summary>
        /// Turns the key song labels on/off
        /// </summary>
        private void ToggleKeySongLabels(bool setActive)
        {
            if (setActive)
            {
                //deactivate key labels
                if (_showNotes)
                    SetShowNotes(false);
                //populate/activate song labels
                SetShowSongLabels(true);
            }
            else
            {
                //deactivate song labels
                SetShowSongLabels(false);
                //reactivate key labels
                if (_showNotes)
                    SetShowNotes(true);
            }
        }
        /// <summary>
        /// Either showing or hiding all song labels
        /// </summary>
        private void SetShowSongLabels(bool show)
        {
            _songLabelsShown = show;
            if (!_songLabelsInstantiated)
            {
                if (!show)
                    return;
                _songLabelsInstantiated = true;
                //we recycle the blueprint as our "stop" label to not waste resources
                _songLabelBlueprint.transform.SetParent(_whiteKeysRoot.GetChild(0), worldPositionStays: true);
                _songLabelBlueprint.transform.localScale = Vector3.one;
                _songLabelBlueprint.transform.localPosition = Vector3.zero;
                _songLabelBlueprint.transform.localRotation = Quaternion.identity;
                int boneCount = _whiteKeysRoot.childCount;
                int currentSong = 0;
                for (int i = 1; i < boneCount; i++)
                {
                    GameObject newLabel = Instantiate(_songLabelBlueprint);
                    newLabel.SetActive(true);
                    _songLabels[currentSong] = newLabel;
                    newLabel.transform.SetParent(_whiteKeysRoot.GetChild(i), worldPositionStays: true);
                    newLabel.transform.localScale = Vector3.one;
                    newLabel.transform.localPosition = Vector3.zero;
                    newLabel.transform.localRotation = Quaternion.identity;
                    Text label = newLabel.GetComponentInChildren<Text>();
                    if (label == null)
                        Debug.LogError("[Piano] Could not fetch text component from white key.");
                    else
                        label.text = _songNameLibrary[currentSong];
                    currentSong++;
                    if (currentSong > _songNameLibrary.Length - 1)
                        break;
                }
            }
            //Showing/hiding UI elements
            foreach (GameObject songLabel in _songLabels)
            {
                if (songLabel != null)
                    songLabel.SetActive(show);
            }
        }
        #endregion GameMenu
        #region GameNetworking
        /// <summary>
        /// Informing all other players that we've started a song
        /// </summary>
        private void SendSongSelection(int songID)
        {
            if (songID < _songMMLLibrary.Length && songID >= 0)
            {
                //No networking in this single-player port; nothing to broadcast.
            }
            else
            {
                Debug.LogError($"[Piano] Invalid song ID {songID} won't be sent over network.");
            }
        }
        /// <summary>
        /// Receiving that someone else started a song
        /// </summary>
        private void ReceiveSongStart(int songID)
        {
            //Remote song events don't exist in this single-player port.
        }
        #endregion GameNetworking
        #region GameUI
        /// <summary>
        /// Refreshing the game stats that are not auto-updated.
        /// This is called during update when the game is running.
        /// </summary>
        private void ResfreshGameStats()
        {
            float totalHitTime = _totalMissedHitTime + _totalCorrectHitTime;
            float precision = totalHitTime == 0 ? 0 : (_totalCorrectHitTime / totalHitTime) * 100;
            _gameUiPrecision.text = $"Precision: {precision.ToString("F2")} %";
            _gameUiScore.text = $"Score: {Mathf.Max(0, Mathf.RoundToInt((_totalGoodHits - _totalWrongHits) * 10 + _totalGoodHits * precision))} XP";
        }
        private void SetGameUImode(string modeName)
        {
            _currentGameModeDisplay.text = $"Mode: {modeName}";
        }
        private void SetGameUIheaders(string header, string description)
        {
            _gameUiHeader.text = header;
            _gameUiDescription.text = description;
        }
        private void UpdateCorrectHitsUI()
        {
            _gameUiGoodHits.text = $"Good Hits: {_totalGoodHits}";
        }
        private void UpdateWrongHitsUI()
        {
            _gameUiWrongHits.text = $"Wrong Hits: {_totalWrongHits}";
        }
        private void UpdateMissedHitsUI()
        {
            _gameUiMissedHits.text = $"Missed Hits: {_totalMissedHits}";
        }
        #endregion GameUI
        #region MovingBlocks
        /// <summary>
        /// Is called during update
        /// Moves all active blocks down
        /// </summary>
        private void MoveActiveBlocks()
        {
            //move all active blocks down by moving the parent which is way cheaper then moving each block
            Vector3 parentPosition = _blockSpawnParent.position;
            parentPosition.y -= Time.deltaTime * _blockSpeedMs;
            _blockSpawnParent.position = parentPosition;
            //now check the timers of all blocks
            for (int i = 0; i < _amountOfSpawnedBlocks; i++)
            {
                float stepEndTime = _blockTimer[i];
                //check if the timer is due
                if (Time.time >= stepEndTime)
                {
                    int currentState = _blockState[i];
                    switch (currentState)
                    {
                        case BLACK_MOVING_TO_HIT:
                        case WHITE_MOVING_TO_HIT:
                            //change color to "hitting"
                            SetBlockMaterial(_activeBlocks[i], _blockMaterialWhenHitting);
                            //this key should get pressed right now
                            _gameKeyShouldBePressedRightNow[_blockKey[i]] = true;
                            //switching to the next state
                            _blockState[i] = currentState == BLACK_MOVING_TO_HIT ? BLACK_MOVING_TO_END_HIT : WHITE_MOVING_TO_END_HIT;
                            break;
                        case BLACK_MOVING_TO_END_HIT:
                        case WHITE_MOVING_TO_END_HIT:
                            //increasing timer to where this step would end
                            stepEndTime += _blockHitDuration[i];
                            if (Time.time < stepEndTime)
                            {
                                if (_currentKeyAngle[_blockKey[i]] >= _KEY_MAX_ROTATION)
                                {
                                    _totalCorrectHitTime += Time.deltaTime;
                                    //detect if it was freshly pressed for the first time
                                    if (!_blockWasHit[i] && !_wasTriggered[i] && Time.time - _lastTriggerTime[i] > 0.1f)
                                    {
                                        _blockWasHit[i] = true;
                                        _totalGoodHits += 1;
                                        UpdateCorrectHitsUI();
                                    }
                                }
                                else
                                {
                                    _totalMissedHitTime += Time.deltaTime;
                                }
                            }
                            else
                            {
                                if (!_blockWasHit[i])
                                {
                                    _totalMissedHits += 1;
                                    UpdateMissedHitsUI();
                                }
                                //this key should no longer be pressed
                                _gameKeyShouldBePressedRightNow[_blockKey[i]] = false;
                                //increase timer by 10cm to ensure that a block still goes a bit further before being destroyed, plus the hit duration
                                _blockTimer[i] += (0.1f / _blockSpeedMs) + _blockHitDuration[i];
                                _blockState[i] = currentState == BLACK_MOVING_TO_END_HIT ? BLACK_MOVING_TO_DESTROY : WHITE_MOVING_TO_DESTROY;
                            }
                            break;
                        case BLACK_MOVING_TO_DESTROY:
                        case WHITE_MOVING_TO_DESTROY:
                            //fetch the block
                            GameObject blockToDestroy = _activeBlocks[i].gameObject;
                            //destroy this block
                            if (blockToDestroy != null)
                                Destroy(blockToDestroy);
                            else
                                Debug.LogError("[Piano] Block-GameObject was invalid, can't destroy it");
                            //remove this block from the array
                            RemoveBlockFromArrays(i);
                            break;
                    }
                }
            }
            //stop moving blocks when the level ended and no blocks are left to move
            if (!_runLevel && _amountOfSpawnedBlocks == 0)
                _moveBlocks = false;
        }
        /// <summary>
        /// Removes a block from the block-array on the defined array index and pushes the stack above it one slot down
        /// </summary>
        private void RemoveBlockFromArrays(int arrayIndex)
        {
            //push stack one down
            for (int i = arrayIndex + 1; i < _amountOfSpawnedBlocks; i++)
            {
                _blockHitDuration[i - 1] = _blockHitDuration[i];
                _blockWasHit[i - 1] = _blockWasHit[i];
                _blockState[i - 1] = _blockState[i];
                _blockTimer[i - 1] = _blockTimer[i];
                _blockKey[i - 1] = _blockKey[i];
                _activeBlocks.SetValue(_activeBlocks.GetValue(i), i - 1);
            }
            _amountOfSpawnedBlocks--;
        }
        /// <summary>
        /// Changing the material of a block
        /// </summary>
        private void SetBlockMaterial(Transform blockTransform, Material newMaterial)
        {
            if (blockTransform == null)
                return;
            Renderer renderer = blockTransform.gameObject.GetComponent<Renderer>();
            Material[] materials = renderer.materials;
            materials[0] = newMaterial;
            renderer.materials = materials;
        }
        #endregion MovingBlocks
        #region Menu
        /// <summary>
        /// Returns if a certain finger position toggled the menu
        /// </summary>
        private bool MenuToggled(Vector3 fingerPos)
        {
            if (_menuToggleArea.Contains(fingerPos))
            {
                if ((fingerPos.y - _fingerThickness) <= _menuToggleHeight + 0.001f)
                {
                    if (_fingerWasAboveMenuToggle && !_menuToggleKeyPressed && (fingerPos.y - _fingerThickness) <= _menuToggleHeight)
                    {
                        _fingerWasAboveMenuToggle = false; //jitter protection
                        _menuToggleKeyPressed = true; //double-finger protection
                        _menuOpen = !_menuOpen;
                        _menuDisplayObj.SetActive(_menuOpen);
                        _menuToggleText.text = !_menuOpen ? "Press to show menu" : "Press to hide menu";
                    }
                    return true;
                }
                else
                {
                    _fingerWasAboveMenuToggle = true;
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if any key of the menu is currently being pressed
        /// </summary>
        private bool MenuButtonPressed(Vector3 fingerPos)
        {
            if (_menuArea.Contains(fingerPos))
            {
                if ((fingerPos.y - _fingerThickness) <= _menuHeight + 0.001f)
                {
                    if (_fingerWasAboveMenu && !_menuKeyPressed && (fingerPos.y - _fingerThickness) <= _menuToggleHeight)
                    {
                        _fingerWasAboveMenu = false;
                        _menuKeyPressed = true;
                        float menuKeyIndexAsFloat = Mathf.Abs(SignedDistancePlanePoint(_menuRefPoint.forward, _menuRefPoint.position, fingerPos) / (_menuArea.size.z / 5));
                        int menuKey = Mathf.FloorToInt(menuKeyIndexAsFloat);
#if MENU_DEBUG
                        Debug.Log($"menuKeyIndexAsFloat: {menuKeyIndexAsFloat}, menuKey: {menuKey}");
#endif
                        switch (menuKey)
                        {
                            case 0:
                                //switch to next game mode
                                _currentGameMode++;
                                if (_currentGameMode > GAME_MODE_END)
                                    _currentGameMode = 0;
                                SwitchToGameMode(_currentGameMode);
                                break;
                            case 1:
                                _showNotes = !_showNotes;
                                SetShowNotes(_showNotes);
                                break;
                            case 2:
                                ToggleNetworkMode();
                                break;
                            case 3:
                                ToggleSlipProtection();
                                break;
                            case 4:
                                ToggleAllFingerMode();
                                break;
                            default:
                                break;
                        }
                    }
                    return true;
                }
                else
                {
                    _fingerWasAboveMenu = true;
                }
            }
            return false;
        }
        /// <summary>
        /// Either showing or hiding all note labels
        /// </summary>
        private void SetShowNotes(bool show)
        {
            if (!_noteLabelsInstantiated)
            {
                if (!show)
                    return;
                _noteLabelsInstantiated = true;
                int whiteBoneCount = _whiteKeysRoot.childCount;
                int blackBoneCount = _blackKeysRoot.childCount;
                int currentNoteLabel = 0;
                _noteLabels = new GameObject[whiteBoneCount + blackBoneCount];
                char[] keys = "CBAGFED".ToCharArray();
                int currentKey = 0;
                for (int i = 0; i < whiteBoneCount; i++)
                {
                    GameObject newNote = Instantiate(_whiteNoteLabelBlueprint);
                    _noteLabels[currentNoteLabel] = newNote;
                    currentNoteLabel++;
                    newNote.SetActive(true);
                    newNote.transform.SetParent(_whiteKeysRoot.GetChild(i), worldPositionStays: true);
                    newNote.transform.localScale = Vector3.one;
                    newNote.transform.localPosition = Vector3.zero;
                    newNote.transform.localRotation = Quaternion.identity;
                    Text label = newNote.GetComponentInChildren<Text>();
                    if (label == null)
                        Debug.LogError("[Piano] Could not fetch text component from white key.");
                    else
                        label.text = keys[currentKey].ToString();
                    currentKey++;
                    if (!(currentKey < keys.Length))
                        currentKey = 0;

                }
                keys = "AGFDC".ToCharArray();
                currentKey = 0;
                for (int i = 0; i < blackBoneCount; i++)
                {
                    GameObject newNote = Instantiate(_blackNoteLabelBlueprint);
                    _noteLabels[currentNoteLabel] = newNote;
                    currentNoteLabel++;
                    newNote.SetActive(true);
                    newNote.transform.SetParent(_blackKeysRoot.GetChild(i), worldPositionStays: true);
                    newNote.transform.localScale = Vector3.one;
                    newNote.transform.localPosition = Vector3.zero;
                    newNote.transform.localRotation = Quaternion.identity;
                    string noteText = keys[currentKey].ToString();
                    Text label = newNote.GetComponentInChildren<Text>();
                    if (label == null)
                        Debug.LogError("[Piano] Could not fetch text component from black key.");
                    else
                        label.text = noteText + "#";
                    currentKey++;
                    if (!(currentKey < keys.Length))
                        currentKey = 0;
                }
            }
            _menuShownotesText.text = show ? "Show notes <color=green>on</color>" : "Show notes <color=red>off</color>";
            if (_songLabelsShown)
            {
                _showNotes = show;
                return;
            }
            //Showing/hiding UI elements
            foreach (GameObject noteLabel in _noteLabels)
            {
                if (noteLabel != null)
                    noteLabel.SetActive(show);
            }
        }
        /// <summary>
        /// Toggle between all and index fingers
        /// </summary>
        private void ToggleAllFingerMode()
        {
            _useAllFingerBones = !_useAllFingerBones;
            _menuFingermodeText.text = _useAllFingerBones ? "<color=green>All</color> fingers & feets" : "<color=red>Only</color> index fingers";
        }
        /// <summary>
        /// Toggle between all and index fingers
        /// </summary>
        private void ToggleNetworkMode()
        {
            _receiveNetworkEvents = !_receiveNetworkEvents;
            _menuNetworkmodeText.text = _receiveNetworkEvents ? "<color=green>Play</color> remote keys" : "<color=red>Only</color> local keys";
        }
        /// <summary>
        /// Toggle between slip protection on and off
        /// </summary>
        private void ToggleSlipProtection()
        {
            _useNoSlipMode = !_useNoSlipMode;
            _menuSlipmodeText.text = _useNoSlipMode ? "Slip protection <color=green>on</color>" : "Slip protection <color=red>off</color>";
            if (_useNoSlipMode)
                for (int i = 0; i < _currentFingerKeyBinding.Length; i++) { _currentFingerKeyBinding[i] = -1; }; //free all fingers again
        }
        #endregion Menu
        #region Interaction
        /// <summary>
        /// We want to run over all fingers to check if they would press down any key no matter which one
        /// </summary>
        private void CheckAllFingers()
        {
            _currentBoneIndex = -1; //needed for slip detection

            //Mouse input: raycast from the camera through the cursor. The hit point stands in
            //for the player's finger position.
            Camera cam = LocalPlayerProxy.Camera;
            if (cam == null)
            {
                if (_fakeHand != null)
                    CheckFinger(_fakeHand.position, _fingerThickness);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Max(_interactionDistance, MAX_AUDIO_DISTANCE) * 2f))
            {
                Vector3 fingerPos = hit.point;
                if (!MenuToggled(fingerPos))
                {
                    _menuToggleKeyPressed = false;
                    if (_menuOpen)
                    {
                        if (!MenuButtonPressed(fingerPos))
                            _menuKeyPressed = false;
                    }
                }
                CheckFinger(fingerPos, fingerThickness: _fingerThickness);
            }
            else
            {
                _menuToggleKeyPressed = false;
                if (_menuOpen)
                    _menuKeyPressed = false;
                if (_useNoSlipMode)
                    _currentFingerKeyBinding[0] = -1; //free the single "finger" (mouse)
            }
        }
        /// <summary>
        /// Checks a single finger bone position and checks which key is being pressed if any
        /// </summary>
        private void CheckFinger(Vector3 fingerPosition, float fingerThickness)
        {
            _currentBoneIndex++;
            //check if finger is inside the white key bounds
            if (_allWhiteKeysBounds.Contains(fingerPosition))
            {
                //calculate the distance to the zero key border
                float whiteKeyIndexAsFloat = SignedDistancePlanePoint(_whiteZeroKeyPlane.right, _whiteZeroKeyPlane.position, fingerPosition) / _whiteKeySize;
                //check if we are inside the black key bounds
                if (_allBlackKeysBounds.Contains(fingerPosition))
                {
                    //first check which white key edge would be nearby
                    int whiteKeyBorder = Mathf.RoundToInt(whiteKeyIndexAsFloat);
                    //check if finger is inside a black key bounds area
                    float distanceToMiddle = whiteKeyIndexAsFloat % 1;
                    if (distanceToMiddle > (1 - BLACK_KEY_SIZE) || distanceToMiddle < BLACK_KEY_SIZE)
                    {
                        //translate this to the real black keycode
                        int blackKeyCode = _blackKeyMapping[whiteKeyBorder];
                        if (blackKeyCode != -1) //-1 = no black key there
                        {
                            CalculateKeyAngle(blackKeyCode + WHITE_KEYS_LENGTH, fingerPosition, keyThiccness: KEY_THICKNESS_BLACK, fingerThickness);
                            return; //when inside a black key bounds, the white key can't be pressed
                        }
                    }
                }
                //reaching this means we are hitting a white key instead
                int keyCode = Mathf.FloorToInt(whiteKeyIndexAsFloat);
                CalculateKeyAngle(keyCode, fingerPosition, keyThiccness: KEY_THICKNESS_WHITE, fingerThickness: _fingerThickness);
            }
            else if (_useNoSlipMode)
            {
                _currentFingerKeyBinding[_currentBoneIndex] = -1; //free this finger from any key
            }
        }
        /// <summary>
        /// Calculates the pressed angle of a given key
        /// </summary>
        private void CalculateKeyAngle(int keyCode, Vector3 fingerPosition, float keyThiccness, float fingerThickness)
        {
            //we need to project the finger position onto a point in the key plane 
            Vector3 projectedHandPos = Vector3.ProjectOnPlane(fingerPosition, _keyProjectionForward);
            //Now we need to get the point from hand pos moved by thiccness towards the projected point
            Vector3 correctedHandPos = fingerPosition + ((projectedHandPos - fingerPosition).normalized * (fingerThickness + keyThiccness));
            Vector3 handPosInLocalKeySpace = _keyRotationZeroRef.InverseTransformPoint(correctedHandPos);
            float newKeyAngle = Mathf.Atan2(handPosInLocalKeySpace.z, handPosInLocalKeySpace.y) * 180 / Mathf.PI;
            newKeyAngle = Mathf.Clamp(newKeyAngle, 0f, _KEY_MAX_ROTATION);
            if (_useNoSlipMode)
            {
                //bind this finger to this key if the pressed angle is greater than half of fully pressed, else unbind it
                if (newKeyAngle < (_KEY_MAX_ROTATION * 0.7f))
                {
                    _currentFingerKeyBinding[_currentBoneIndex] = -1; //free this finger from any key
                }
                else
                {
                    int currentBindingKey = _currentFingerKeyBinding[_currentBoneIndex];
                    //no existing binding or old keybinding was black but new key is white
                    if (currentBindingKey == -1)
                    {
                        _currentFingerKeyBinding[_currentBoneIndex] = keyCode; //bind this finger to this key
                    }
                    else if (currentBindingKey != keyCode)
                    {
                        //hold down the old key instead
                        //if the new angle is less than the current angle, we only move it down by time 
                        float nextAngleOldKey = _currentKeyAngle[currentBindingKey];
                        nextAngleOldKey = Mathf.Max(0, nextAngleOldKey - ((Time.deltaTime) * MOVE_BACK_SPEED));
                        if (newKeyAngle >= nextAngleOldKey)
                        {
                            if (!_isHeld[currentBindingKey])
                            {
                                _isHeld[currentBindingKey] = true;
                                _isHeldCount++;
                            }
                            _currentKeyAngle[currentBindingKey] = newKeyAngle;
                        }
                        return; //protect the other key from being pressed by a finger that is currently holding down the old key instead
                    }
                }
            }
            //if the new angle is less than the current angle, we only move it down by time 
            float nextAngle = _currentKeyAngle[keyCode];
            nextAngle = Mathf.Max(0, nextAngle - ((Time.deltaTime) * MOVE_BACK_SPEED));
            if (newKeyAngle >= nextAngle)
            {
                if (!_isHeld[keyCode])
                {
                    _isHeld[keyCode] = true;
                    _isHeldCount++;
                }
                _currentKeyAngle[keyCode] = newKeyAngle;
            }
        }
        /// <summary>
        /// Performs the OnInteract-equivalent trigger
        /// </summary>
        private void KeyPress(int key)
        {
            if (_amountOfSpawnedAudioSources < MAX_AUDIO_SOURCES)
            {
                GameObject gameObj = Instantiate(_audioSourceBlueprint);
                gameObj.SetActive(true);
                //TODO: Possible frametime boost by making this a static calculation without object reference
                Transform keyBone = key < WHITE_KEYS_LENGTH ? _whiteKeysRoot.GetChild(key) : _blackKeysRoot.GetChild(key - WHITE_KEYS_LENGTH);
                Vector3 audioPosition = _audioSourceBlueprint.transform.position;
                audioPosition.x = keyBone.position.x;
                gameObj.transform.position = audioPosition;
                AudioSource audioSrc = gameObj.GetComponent<AudioSource>();
                _audioSpawnTimes[_amountOfSpawnedAudioSources] = Time.timeSinceLevelLoad;
                _audioSourcesSpawned[_amountOfSpawnedAudioSources] = audioSrc;
                _amountOfSpawnedAudioSources++;
                int midiKey = key < WHITE_KEYS_LENGTH ? _whiteKeyToMidi[key] : _blackKeyToMidi[key - WHITE_KEYS_LENGTH];
                audioSrc.pitch = KeyToPitch(midiKey);
#if UDON_VOLUME_CURVES
                    float volumeAtStart = (1f - ((key / 2f) / ALL_KEYS_LENGHT) * 0.7f) * 0.4f; //21 lowest, 108 highest
                    audioSrc.volume = volumeAtStart;
#else
                float volumeAtStart = 0.4f + ((midiKey / 108f) * 0.6f); //21 lowest, 108 highest
                //Debug.Log($"Midikey {midiKey} translates to {volumeAtStart} volume at start");
                Animator animator = gameObj.GetComponent<Animator>();
                //this is a hack to make deep frequencies less loud while using an animator which is way more performant than doing the same in Udon (like in pre-V1.5)
                animator.Play("ReduceVolumeLinear", 0, 1f - volumeAtStart);
                //VRC_SpatialAudioSource is not supported in Udon yet, else we could use this for the master gain and keep the same playtime
                /* 
                float linearVolume = (1f - ((key / 2f) / ALL_KEYS_LENGHT) * 0.7f) * 0.4f; //21 lowest, 108 highest
                VRC_SpatialAudioSource vrcSpatialAudioSource = (VRC_SpatialAudioSource)gameObj.GetComponent(typeof(VRC_SpatialAudioSource));
                vrcSpatialAudioSource.Gain = (20.0f * Mathf.Log10(volumeAtStart)) + 10f;
                */
#endif
                audioSrc.Play();
            }
            else if (Time.timeSinceLevelLoad - _exceededLimitPrintedLastTime > 3f)
            {
                Debug.LogError("[Piano] Exceeded limit of audio sources (this error is only printed each 3 seconds)");
                _exceededLimitPrintedLastTime = Time.timeSinceLevelLoad;
                return;
            }
        }
        /// <summary>
        /// Checks for all keys if they need to be moved somewhere based on previous finger bone checks
        /// </summary>
        private void RunAllKeys()
        {
            //doing this gives us a huge frametime boost since we don't need to loop over the full key array
            //when no key is/was pressed which would be extremely expensive in Udon
            if (_isHeldCount == 0 && _wasInBoundCount == 0)
                return;
            Transform keyBone;
            bool isHeld;
            bool wasInBound;
            for (int i = 0; i < ALL_KEYS_LENGHT; i++)
            {
                //minimize array access
                isHeld = _isHeldCount > 0 ? _isHeld[i] : false;
                wasInBound = _wasInBoundCount > 0 ? _wasInBound[i] : false;
#if TEST_DEBUG
                _isHeldDebugCount += isHeld ? 1 : 0;
                _wasInBoundDebugCount += wasInBound ? 1 : 0;
#endif
                //minimize bone access
                if (isHeld || wasInBound)
                    keyBone = i < WHITE_KEYS_LENGTH ? _whiteKeysRoot.GetChild(i) : _blackKeysRoot.GetChild(i - WHITE_KEYS_LENGTH);
                else
                    continue;
                //check if hand is in bounds
                if (isHeld)
                {
                    //resetting isHeld since this is now handled
                    _isHeld[i] = false;
                    _isHeldCount--;
                    if (!wasInBound)
                    {
                        _wasInBound[i] = true;
                        _wasInBoundCount++;
                    }
                    float newAngle = _currentKeyAngle[i];
                    Vector3 keyLocalRotation = keyBone.localRotation.eulerAngles;
                    keyLocalRotation.x = -newAngle;
                    keyBone.transform.localRotation = Quaternion.Euler(keyLocalRotation);
                    //trigger action when limit reached for the first time and the last trigger time is at least x seconds ago
                    if (newAngle >= _KEY_MAX_ROTATION && !_wasTriggered[i] && Time.time - _lastTriggerTime[i] > MINIMUM_TIME_BETWEEN_KEYPRESS)
                    {
                        _lastTriggerTime[i] = Time.time;
                        //ButtonDownEvent
                        KeyPress(i);
                        _wasTriggered[i] = true;
                        //tell the game mode that this key got pressed
                        if (_forwardKeysToGameMode)
                            KeyPressDuringGame(i);
                    }
                }
                else if (wasInBound)
                {
                    //move it slowly back
                    //Calculate the new key angle
                    float newAngle = _currentKeyAngle[i];
                    newAngle = Mathf.Max(0, newAngle - (Time.deltaTime * MOVE_BACK_SPEED));
                    //Apply the rotation to the key
                    Vector3 keyLocalRotation = keyBone.transform.localRotation.eulerAngles;
                    keyLocalRotation.x = -newAngle;
                    keyBone.transform.localRotation = Quaternion.Euler(keyLocalRotation);
                    _currentKeyAngle[i] = newAngle;
                    //Key has returned, stop moving
                    if (newAngle == 0)
                    {
                        _wasInBound[i] = false;
                        _wasInBoundCount--;
                    }
                }
                //wait until the key moved x percent back to set _wastriggered to false
                if (_wasTriggered[i])
                {
                    if (_currentKeyAngle[i] < _KEY_MAX_ROTATION * 0.8f)
                    {
                        _wasTriggered[i] = false;
                    }
                }
            }
        }
        #endregion Interaction
        #region Calibration
        /// <summary>
        /// This is externally called after setting _avatarHeight (happening after localPlayer changed their avatar)
        /// </summary>
        public void _OnAvatarChanged()
        {
            if (!_hasFinishedStart)
                return; //distances are made for a 1.3m avatar as a reference
            _fingerThickness = FINGER_THICKNESS_DEFAULT * _avatarHeight / 1.3f;
            _headThickness = HEAD_THICKNESS_DEFAULT * _avatarHeight / 1.3f;
            _footThickness = FOOT_THICKNESS_DEFAULT * _avatarHeight / 1.3f;
            _interactionDistance = DEFAULT_INTERACTION_DISTANCE * _avatarHeight / 1.3f;
            //Mouse input needs no per-bone calibration.
        }
        #endregion Calibration
        #region MIDI
        /// <summary>
        /// Is called from the VRC Midi Listener under the piano script. See https://docs.vrchat.com/v2021.1.4/docs/midi
        /// </summary>
        /// <param name="channel">Midi Channel that received the event, 0-15.</param>
        /// <param name="number">Note number from 0-127 (the midi Device may not output the full range)</param>
        /// <param name="velocity">Number from 0-127 representing the speed at which the note was triggered, if supported by the midi device.</param>
        public void MidiNoteOn(int channel, int number, int velocity)
        {
            int key = ConvertMidiNoteToKeyID(number);
            if (key < 0) //MIDI has 128 keys but the piano only has 88, so ConvertMidiNoteToKeyID() can return -1
                return;
            if (!_isHeld[key])
            {
                _isHeld[key] = true;
                _isHeldCount++;
            }
            _currentKeyAngle[key] = _KEY_MAX_ROTATION;
            //Note: We could lock this key and wait for MidiNoteOff to unlock it, but this would cause
            //a significant performance overhead for everyone only to have a slight visual improvement 
            //for a few local Midi players which is not worth it IMHO given how expensive Udon VM instructions are
        }
        #endregion MIDI
        #region KeyNetworking
        /// <summary>
        /// Sending a keypress to all other players
        /// </summary>
        /// <param name="key"></param>
        private void SendKeyOverNetwork(int key)
        {
            _lastSendTime[key] = Time.timeSinceLevelLoad;
            //No networking in this single-player port.
        }
        /// <summary>
        /// Receiving a key press from another player
        /// </summary>
        private void ReceiveKeyFromNetwork(int key)
        {
            //Remote key events don't exist in this single-player port.
        }
        #endregion KeyNetworking
        #region RPCs
        public void K0()
        {
            ReceiveKeyFromNetwork(key: 0);
        }
        public void K1()
        {
            ReceiveKeyFromNetwork(key: 1);
        }
        public void K2()
        {
            ReceiveKeyFromNetwork(key: 2);
        }
        public void K3()
        {
            ReceiveKeyFromNetwork(key: 3);
        }
        public void K4()
        {
            ReceiveKeyFromNetwork(key: 4);
        }
        public void K5()
        {
            ReceiveKeyFromNetwork(key: 5);
        }
        public void K6()
        {
            ReceiveKeyFromNetwork(key: 6);
        }
        public void K7()
        {
            ReceiveKeyFromNetwork(key: 7);
        }
        public void K8()
        {
            ReceiveKeyFromNetwork(key: 8);
        }
        public void K9()
        {
            ReceiveKeyFromNetwork(key: 9);
        }
        public void K10()
        {
            ReceiveKeyFromNetwork(key: 10);
        }
        public void K11()
        {
            ReceiveKeyFromNetwork(key: 11);
        }
        public void K12()
        {
            ReceiveKeyFromNetwork(key: 12);
        }
        public void K13()
        {
            ReceiveKeyFromNetwork(key: 13);
        }
        public void K14()
        {
            ReceiveKeyFromNetwork(key: 14);
        }
        public void K15()
        {
            ReceiveKeyFromNetwork(key: 15);
        }
        public void K16()
        {
            ReceiveKeyFromNetwork(key: 16);
        }
        public void K17()
        {
            ReceiveKeyFromNetwork(key: 17);
        }
        public void K18()
        {
            ReceiveKeyFromNetwork(key: 18);
        }
        public void K19()
        {
            ReceiveKeyFromNetwork(key: 19);
        }
        public void K20()
        {
            ReceiveKeyFromNetwork(key: 20);
        }
        public void K21()
        {
            ReceiveKeyFromNetwork(key: 21);
        }
        public void K22()
        {
            ReceiveKeyFromNetwork(key: 22);
        }
        public void K23()
        {
            ReceiveKeyFromNetwork(key: 23);
        }
        public void K24()
        {
            ReceiveKeyFromNetwork(key: 24);
        }
        public void K25()
        {
            ReceiveKeyFromNetwork(key: 25);
        }
        public void K26()
        {
            ReceiveKeyFromNetwork(key: 26);
        }
        public void K27()
        {
            ReceiveKeyFromNetwork(key: 27);
        }
        public void K28()
        {
            ReceiveKeyFromNetwork(key: 28);
        }
        public void K29()
        {
            ReceiveKeyFromNetwork(key: 29);
        }
        public void K30()
        {
            ReceiveKeyFromNetwork(key: 30);
        }
        public void K31()
        {
            ReceiveKeyFromNetwork(key: 31);
        }
        public void K32()
        {
            ReceiveKeyFromNetwork(key: 32);
        }
        public void K33()
        {
            ReceiveKeyFromNetwork(key: 33);
        }
        public void K34()
        {
            ReceiveKeyFromNetwork(key: 34);
        }
        public void K35()
        {
            ReceiveKeyFromNetwork(key: 35);
        }
        public void K36()
        {
            ReceiveKeyFromNetwork(key: 36);
        }
        public void K37()
        {
            ReceiveKeyFromNetwork(key: 37);
        }
        public void K38()
        {
            ReceiveKeyFromNetwork(key: 38);
        }
        public void K39()
        {
            ReceiveKeyFromNetwork(key: 39);
        }
        public void K40()
        {
            ReceiveKeyFromNetwork(key: 40);
        }
        public void K41()
        {
            ReceiveKeyFromNetwork(key: 41);
        }
        public void K42()
        {
            ReceiveKeyFromNetwork(key: 42);
        }
        public void K43()
        {
            ReceiveKeyFromNetwork(key: 43);
        }
        public void K44()
        {
            ReceiveKeyFromNetwork(key: 44);
        }
        public void K45()
        {
            ReceiveKeyFromNetwork(key: 45);
        }
        public void K46()
        {
            ReceiveKeyFromNetwork(key: 46);
        }
        public void K47()
        {
            ReceiveKeyFromNetwork(key: 47);
        }
        public void K48()
        {
            ReceiveKeyFromNetwork(key: 48);
        }
        public void K49()
        {
            ReceiveKeyFromNetwork(key: 49);
        }
        public void K50()
        {
            ReceiveKeyFromNetwork(key: 50);
        }
        public void K51()
        {
            ReceiveKeyFromNetwork(key: 51);
        }
        public void K52()
        {
            ReceiveKeyFromNetwork(key: 52);
        }
        public void K53()
        {
            ReceiveKeyFromNetwork(key: 53);
        }
        public void K54()
        {
            ReceiveKeyFromNetwork(key: 54);
        }
        public void K55()
        {
            ReceiveKeyFromNetwork(key: 55);
        }
        public void K56()
        {
            ReceiveKeyFromNetwork(key: 56);
        }
        public void K57()
        {
            ReceiveKeyFromNetwork(key: 57);
        }
        public void K58()
        {
            ReceiveKeyFromNetwork(key: 58);
        }
        public void K59()
        {
            ReceiveKeyFromNetwork(key: 59);
        }
        public void K60()
        {
            ReceiveKeyFromNetwork(key: 60);
        }
        public void K61()
        {
            ReceiveKeyFromNetwork(key: 61);
        }
        public void K62()
        {
            ReceiveKeyFromNetwork(key: 62);
        }
        public void K63()
        {
            ReceiveKeyFromNetwork(key: 63);
        }
        public void K64()
        {
            ReceiveKeyFromNetwork(key: 64);
        }
        public void K65()
        {
            ReceiveKeyFromNetwork(key: 65);
        }
        public void K66()
        {
            ReceiveKeyFromNetwork(key: 66);
        }
        public void K67()
        {
            ReceiveKeyFromNetwork(key: 67);
        }
        public void K68()
        {
            ReceiveKeyFromNetwork(key: 68);
        }
        public void K69()
        {
            ReceiveKeyFromNetwork(key: 69);
        }
        public void K70()
        {
            ReceiveKeyFromNetwork(key: 70);
        }
        public void K71()
        {
            ReceiveKeyFromNetwork(key: 71);
        }
        public void K72()
        {
            ReceiveKeyFromNetwork(key: 72);
        }
        public void K73()
        {
            ReceiveKeyFromNetwork(key: 73);
        }
        public void K74()
        {
            ReceiveKeyFromNetwork(key: 74);
        }
        public void K75()
        {
            ReceiveKeyFromNetwork(key: 75);
        }
        public void K76()
        {
            ReceiveKeyFromNetwork(key: 76);
        }
        public void K77()
        {
            ReceiveKeyFromNetwork(key: 77);
        }
        public void K78()
        {
            ReceiveKeyFromNetwork(key: 78);
        }
        public void K79()
        {
            ReceiveKeyFromNetwork(key: 79);
        }
        public void K80()
        {
            ReceiveKeyFromNetwork(key: 80);
        }
        public void K81()
        {
            ReceiveKeyFromNetwork(key: 81);
        }
        public void K82()
        {
            ReceiveKeyFromNetwork(key: 82);
        }
        public void K83()
        {
            ReceiveKeyFromNetwork(key: 83);
        }
        public void K84()
        {
            ReceiveKeyFromNetwork(key: 84);
        }
        public void K85()
        {
            ReceiveKeyFromNetwork(key: 85);
        }
        public void K86()
        {
            ReceiveKeyFromNetwork(key: 86);
        }
        public void K87()
        {
            ReceiveKeyFromNetwork(key: 87);
        }
        //All song RPCs
        public void S0()
        {
            ReceiveSongStart(songID: 0);
        }
        public void S1()
        {
            ReceiveSongStart(songID: 1);
        }
        public void S2()
        {
            ReceiveSongStart(songID: 2);
        }
        public void S3()
        {
            ReceiveSongStart(songID: 3);
        }
        public void S4()
        {
            ReceiveSongStart(songID: 4);
        }
        public void S5()
        {
            ReceiveSongStart(songID: 5);
        }
        public void S6()
        {
            ReceiveSongStart(songID: 6);
        }
        public void S7()
        {
            ReceiveSongStart(songID: 7);
        }
        public void S8()
        {
            ReceiveSongStart(songID: 8);
        }
        public void S9()
        {
            ReceiveSongStart(songID: 9);
        }
        public void S10()
        {
            ReceiveSongStart(songID: 10);
        }
        public void S11()
        {
            ReceiveSongStart(songID: 11);
        }
        public void S12()
        {
            ReceiveSongStart(songID: 12);
        }
        public void S13()
        {
            ReceiveSongStart(songID: 13);
        }
        public void S14()
        {
            ReceiveSongStart(songID: 14);
        }
        public void S15()
        {
            ReceiveSongStart(songID: 15);
        }
        public void S16()
        {
            ReceiveSongStart(songID: 16);
        }
        public void S17()
        {
            ReceiveSongStart(songID: 17);
        }
        public void S18()
        {
            ReceiveSongStart(songID: 18);
        }
        public void S19()
        {
            ReceiveSongStart(songID: 19);
        }
        public void S20()
        {
            ReceiveSongStart(songID: 20);
        }
        public void S21()
        {
            ReceiveSongStart(songID: 21);
        }
        public void S22()
        {
            ReceiveSongStart(songID: 22);
        }
        public void S23()
        {
            ReceiveSongStart(songID: 23);
        }
        public void S24()
        {
            ReceiveSongStart(songID: 24);
        }
        public void S25()
        {
            ReceiveSongStart(songID: 25);
        }
        public void S26()
        {
            ReceiveSongStart(songID: 26);
        }
        public void S27()
        {
            ReceiveSongStart(songID: 27);
        }
        public void S28()
        {
            ReceiveSongStart(songID: 28);
        }
        public void S29()
        {
            ReceiveSongStart(songID: 29);
        }
        public void S30()
        {
            ReceiveSongStart(songID: 30);
        }
        public void S31()
        {
            ReceiveSongStart(songID: 31);
        }
        public void S32()
        {
            ReceiveSongStart(songID: 32);
        }
        public void S33()
        {
            ReceiveSongStart(songID: 33);
        }
        public void S34()
        {
            ReceiveSongStart(songID: 34);
        }
        public void S35()
        {
            ReceiveSongStart(songID: 35);
        }
        public void S36()
        {
            ReceiveSongStart(songID: 36);
        }
        public void S37()
        {
            ReceiveSongStart(songID: 37);
        }
        public void S38()
        {
            ReceiveSongStart(songID: 38);
        }
        public void S39()
        {
            ReceiveSongStart(songID: 39);
        }
        public void S40()
        {
            ReceiveSongStart(songID: 40);
        }
        public void S41()
        {
            ReceiveSongStart(songID: 41);
        }
        public void S42()
        {
            ReceiveSongStart(songID: 42);
        }
        public void S43()
        {
            ReceiveSongStart(songID: 43);
        }
        public void S44()
        {
            ReceiveSongStart(songID: 44);
        }
        public void S45()
        {
            ReceiveSongStart(songID: 45);
        }
        public void S46()
        {
            ReceiveSongStart(songID: 46);
        }
        public void S47()
        {
            ReceiveSongStart(songID: 47);
        }
        public void S48()
        {
            ReceiveSongStart(songID: 48);
        }
        public void S49()
        {
            ReceiveSongStart(songID: 49);
        }
        public void S50()
        {
            ReceiveSongStart(songID: 50);
        }
        public void S51()
        {
            ReceiveSongStart(songID: 51);
        }
        public void S52()
        {
            ReceiveSongStart(songID: 52);
        }
        public void S53()
        {
            ReceiveSongStart(songID: 53);
        }
        public void S54()
        {
            ReceiveSongStart(songID: 54);
        }
        public void S55()
        {
            ReceiveSongStart(songID: 55);
        }
        public void S56()
        {
            ReceiveSongStart(songID: 56);
        }
        public void S57()
        {
            ReceiveSongStart(songID: 57);
        }
        public void S58()
        {
            ReceiveSongStart(songID: 58);
        }
        public void S59()
        {
            ReceiveSongStart(songID: 59);
        }
        public void S60()
        {
            ReceiveSongStart(songID: 60);
        }
        public void S61()
        {
            ReceiveSongStart(songID: 61);
        }
        public void S62()
        {
            ReceiveSongStart(songID: 62);
        }
        public void S63()
        {
            ReceiveSongStart(songID: 63);
        }
        public void S64()
        {
            ReceiveSongStart(songID: 64);
        }
        public void S65()
        {
            ReceiveSongStart(songID: 65);
        }
        public void S66()
        {
            ReceiveSongStart(songID: 66);
        }
        public void S67()
        {
            ReceiveSongStart(songID: 67);
        }
        public void S68()
        {
            ReceiveSongStart(songID: 68);
        }
        public void S69()
        {
            ReceiveSongStart(songID: 69);
        }
        public void S70()
        {
            ReceiveSongStart(songID: 70);
        }
        public void S71()
        {
            ReceiveSongStart(songID: 71);
        }
        public void S72()
        {
            ReceiveSongStart(songID: 72);
        }
        public void S73()
        {
            ReceiveSongStart(songID: 73);
        }
        public void S74()
        {
            ReceiveSongStart(songID: 74);
        }
        public void S75()
        {
            ReceiveSongStart(songID: 75);
        }
        public void S76()
        {
            ReceiveSongStart(songID: 76);
        }
        public void S77()
        {
            ReceiveSongStart(songID: 77);
        }
        public void S78()
        {
            ReceiveSongStart(songID: 78);
        }
        public void S79()
        {
            ReceiveSongStart(songID: 79);
        }
        public void S80()
        {
            ReceiveSongStart(songID: 80);
        }
        public void S81()
        {
            ReceiveSongStart(songID: 81);
        }
        public void S82()
        {
            ReceiveSongStart(songID: 82);
        }
        public void S83()
        {
            ReceiveSongStart(songID: 83);
        }
        public void S84()
        {
            ReceiveSongStart(songID: 84);
        }
        public void S85()
        {
            ReceiveSongStart(songID: 85);
        }
        public void S86()
        {
            ReceiveSongStart(songID: 86);
        }
        #endregion RPCs
        #region CommonFunctions
        /// <summary>
        /// Get the shortest distance between a point and a plane. The output is signed so it holds information
        /// as to which side of the plane normal the point is.
        /// </summary>
        private float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            return Vector3.Dot(planeNormal, (point - planePoint));
        }
        #endregion CommonFunctions
        #region SongLibrary
        /// <summary>
        /// Setting up the song library when the game is first activated
        /// Use this template to generate the code: https://docs.google.com/spreadsheets/d/13itswFr_b923GQUlReXyhdP-evB8-sscBA9R6NsaFUY/edit#gid=0
        /// </summary>
        private void SetupSongLibrary()
        {
            _songLibraryIsSetup = true;
            //only 51 songs are allowed!
            int songCount = 44;
            _songMMLLibrary = new string[songCount];
            _songNameLibrary = new string[songCount];
            _songLabels = new GameObject[songCount];
            //populate MML array on runtime
            _songMMLLibrary[0] = "t150v127<g>cl8cdc<bl4aaa>dl8dedcl4<bgg>el8efedc4<a4ggl4a>dc-c2<g>cl8cdc<bl4aaa>dl8dedcl4<bgg>el8efedc4<a4ggl4a>dc-c2<g>ccc<b2bb+bag2>dedcg<gg8g8a>dc-c2<g>cl8cdc<bl4aaa>dl8dedcl4<bgg>el8efedc4<a4ggl4a>dc-c1";
            _songMMLLibrary[1] = "v127t135>c8c8c4f8f8f4e8f8g8a8a+8g8a2>c4d8<a+8a8f8g4f2.c8c8c4f8f8f4e8f8g8a8a+8g8a2>c4<g8a8a+4a8a+8>c4d8<a+8a8f8g4f2.c8c8c4f8f8f4e8f8g8a8a+8g8a2>c4<g8a8a+4>c4<g8a8a+4a8a+8>c4d8<a+8a8f8g4f2.c8c8c4f8f8f4e8f8g8a8a+8g8a2>c4<g8a8a+4>c4<g8a8a+4>c4<g8a8a+4a8a+8>c4d8<a+8a8f8g4f2.c8c8c4f8f8f4e8f8g8a8a+8g8a2>c2d4<b4>c1c8<a+8a8g8f4a+4d4f4g8f8e8d8c4a8a+8>c4d8<a+8a8f8g4f1";
            _songMMLLibrary[2] = "t180L4O4V100dc8<bagabg.b8b+8a8ba8gf+g2>dc8<bagabg.b8b+8a8ba8gf+r32g2l1.r2r8r16r32l4>gggg8g8gef+ggggggbf+ggggggabb2a2r1rgg2ggbggg8g8ggbgg8g8f+ggggg8g8ggbb2a2baaggr1r1g8g8ggbggggg8g8ggggbggggggggbb2a2r1rf+ggggaf+ggggbggf+ggggg2gbb2a2r>ddc<br2bbr2abl8ggg4ggbb4rl4b>d<br2abr2abgg8g8g>dc<bagr2gb8bbgr2r4r8gbb8bb8>d<br2gbb8b8bggr2gbbbggaf+f+g";
            _songMMLLibrary[3] = "T190b4b4b2b4b4b2b4>d4<g4.a8b1>c4c4c4.c8c4<b4b4b8b8b4a4a4b4a2>d2<b4b4b2b4b4b2b4>d4<g4.a8b1>c4c4c4.c8c4<b4b4b8b8>d4d4c4<a4g2.d4d4b4a4g4d2.d8d8d4b4a4g4e2.e4e4>c4<b4a4f+2.>d4e4d4c4<a4b2.d4d4b4a4g4d2.d8d8d4b4a4g4e2.e4e4>c4<b4a4>d4d4d4d4e4d4c4<a4g2>d2<b4b4b2b4b4b2b4>d4<g4.a8b1>c4c4c4.c8c4<b4b4b8b8b4a4a4b4a2>d2<b4b4b2b4b4b2b4>d4<g4.a8b1>c4c4c4.c8c4<b4b4b8b8>d4d4c4<a4g1,d1d1d1<g1g1g1a1a2>d2d1d1d1<g1g1g1>d1d1<b1b1b1g1g1>d1d1d1<b1b1b1g1g1>d1d1d1d1d1d1<g1g1g1a1a2>d2d1d1d1<g1g1g1>d1d1;";
            _songMMLLibrary[4] = "t110c4f8f8f4g4a8a8a4a4g8a8a+4e4g4f4c4f8f8f4g4a8a8a4a4g8a8a+4e4g4f4,o3c4f8f8f4g4a8a8a4a4g8a8a+4e4g4f4c4f8f8f4g4a8a8a4a4g8a8a+4e4g4f4,o4c4f8f8f4g4a8a8a4a4g8a8a+4e4g4f4c4f8f8f4g4a8a8a4a4g8a8a+4e4g4f4";
            _songMMLLibrary[5] = "MML@t100o3f8>c8g8a4.f4<f8>c8f8g8a8g8f8<a8d8a8>d8e8f2<d8a8>d8e8f8e8d8<a8g8>d8a8a+4.d4<g1>c2c2<c2&c8c8d8e8f8>c8g8a4.f4<f8>c8f8g8a8g8f8<a8d8a8>d8e8f2<d8a8>d8e8f8e8d8<a8g8>d8a8a+4.d4<g1>c2c2<c2&c8c8d8e8f8>c8g8a4g8f8c8<f8>c8g8a4g8f8c8<d8a8>d8e8f8e8d8<a8d8a8>d8e8f4<a4g8>d8g8a4.d4<g8>d8g8a8a+2c4.c4.c4c4.c4.c4<f8>c8f8g8a4f4<f2.e4d8a8>d8e8f8e8d8<a8d2&d8d8a8>d8<g8>d8a8a+4.d4<g8>d8a8a+4g8e8d8c1<c2&c8c8d8e8f8>c8g8a4.f4<f8>c8f8g8a8g8f8<a8d8a8>d8e8f2<d8a8>d8e8f8e8d8<a8g8>d8a8a+4.d4<g1>c2.c4<c2&c8c8d8e8f8>c8f8g8a2>g4.g4f2.,t100o5g4.g4f4c8g8g8a8f4.d8d8g8g8a4f4.f8e8f8e8d2&d8a4.g2d8a8a+8a8g4.<a+4>e8e8f8e4f4.e4c2.t100g4.g4f4c8g8g8a8f4.d8d8g8g8a4f4.f8e8f8e8d2&d8a4.g2d8a8a+8a8g4.<a+4>e8e8f8e4f4.e4c2.g4.g4f4c8>c4.<a4g8f4<a2>g8f4f8>d8d4<a4g4f4.a8d8f8f4.a8a16a8.e8f8g4.f8f8f8f8f4.f8e8e8f8e4c4.<a2>g8f4f8e8f8e8f8e8f4.<a4.>g4f8f8f8e8f4f8e8f4.a8g8f8g4.a8a8a8g8f8g4g8g8f8e8f4>c4<f8g8f8e8e8f8g2&g8c4.c4<a4g8>e8e8f8c4.c8c8d8d8f4f4.c8c8d8c8<a2&a8>a4.g2d8d8d8d8d4.<a+4>e8e8f8e4f4.e4c2.<a2&a8>c8f8g8>g4.g4f2.;";
            _songMMLLibrary[6] = "t120e2.&e8.&e64r32.f8.&f32.r64e8.&e32.r64d+8.&d+32.r64e8.&e32.r64f2.&f8.&f64r32.f+8.&f+32.r64g2&g8.&g32r4r32a8.&a32.r64b8.&b32.r64>c8.&c32.r64d8.&d32.r64c8.&c32.r64<b4&b16.&b64r64a8g2.&g8.&g64r32.g8.&g32.r4r64c8.&c32.r64d8.&d32.r64e4.&e16.r32e4.&e16.r32e8.&e32.r64e4.&e16.r32e8.&e32.r64c4.&c16.r32c4.&c16.r32c8.&c32.r64c4.&c16.r32c8.&c32.r64c2.&c8.&c64r32.d8.&d32.r64c8.&c32.r64d8.&d32.r64c8.&c32.r64c1<b2&b8.&b32r4r32>e2.&e8.&e64r32.f8.&f32.r64e8.&e32.r64d+8.&d+32.r64e8.&e32.r64f2.&f8.&f64r32.f+8.&f+32.r64g2&g8.&g32r4r32a8.&a32.r64b8.&b32.r64>c8.&c32.r64d8.&d32.r64c8.&c32.r64<b4&b16.&b64r64a8g1g8.&g32.r4r64c8.&c32.r64d8.&d32.r64e4.&e16.r32e4.&e16.r32e8.&e32.r64e4.&e16.r32e8.&e32.r64a2.&a8.&a64r32.g+4.&g+16.r4r32c8d8e4.&e16.r32e4.&e16.r32a4&a16.&a64r64<b8b8.&b32.r64b8.&b32.r64>c2.&c8.&c64r,t120<g2.&g8.&g64r32.a8.&a32.r64g8.&g32.r64f+8.&f+32.r64g8.&g32.r64a2.&a8.&a64r32.a+8.&a+32.r64b2&b8.&b32r4r32>c8.&c32.r64<b8.&b32.r64a8.&a32.r64f8.&f32.r64a8.&a32.r64>d4&d16.&d64r64c8<b4>c4<f2g8.&g32.r4r64>c8.&c32.r64<b8.&b32.r64>c4<g8.&g32.r64>c4<b8.&b32.r64a+8.&a+32.r64g4.&g16.r32c8.&c32.r64f4c4g4f8.&f32.r64f8.&f32.r64f4.&f16.r32f8.&f32.r64g2.&g8.&g64r32.a8.&a32.r64g8.&g32.r64f+8.&f+32.r64f+8.&f+32.r64f1g2&g8.&g32r4r32g2.&g8.&g64r32.a8.&a32.r64g8.&g32.r64f+8.&f+32.r64g8.&g32.r64a2.&a8.&a64r32.a+8.&a+32.r64b2&b8.&b32r4r32>c8.&c32.r64<b8.&b32.r64a8.&a32.r64f8.&f32.r64a8.&a32.r64>d4&d16.&d64r64c8<b4>c4<g2g8.&g32.r4r64>c8.&c32.r64<b8.&b32.r64>c4.&c16.r32c8.&c32.r64<b8.&b32.r64a+8.&a+32.r64a+4.&a+16.r32a+8.&a+32.r64f4f2f4f4.&f16.r4r32>c8<b8>c4.&c16.r32<g4.&g16.r32f4&f16.&f64r64g8g8.&g32.r64g8.&g32.r64c2.&c8.&c64r1";
            _songMMLLibrary[7] = "2l8>gf+eef+1&f+4.<a>gf+el4ef+.de8<a1a8>ef+8g.e8c+d.e<a8a>f+1&f+.l8gf+eef+1&f+4.<a>gf+ee4.f+d4.e<a1&a>e4f+g4.ec+4.de4<a>defedc4.<aa+>c4f4eddcdcc4c4<aa+>c4f4gfeddef4f4gaa+a+a4g4fgaag4f4dcdffe4ef+l1.f+";
            _songMMLLibrary[8] = "t89g8.&g32.r64g8.&g32.r64g8.&g32.r64b8.&b32.r64>e8.&e32.r64e8.&e32.r64e8.&e32.r64d8.&d32.r64<b8.&b32.r64b8.&b32.r64b8.&b32.r64b8.&b32.r64f+8.&f+32.r64f+8.&f+32.r64f+8.&f+32.r64e8.&e32.r8r64g8g8e8g8e8g8a8b8.&b32.r64g8g8d8b4.&b16.r2r32g8g8g8f+4&f+16.&f+64r64f+8f+8e8g8.";
            _songMMLLibrary[9] = "t100V120l8o2a>e<a>e<a>d+4.<a>d<a>d<a>f4.<a>e<a>e<a>d+4.<a>d<a>d<a>f4.<a>e<a>e<al4>d+.<aal16eeeeef+b8l4aaa2aaa2aaa2ddl8c+g+O3C+O2g+f+f+4l16f+f+g+8g+4g+g+a+8a+4a+a+b8b8b>c+d+f+<f+8f+4f+f+g+8g+4g+g+a+8a+4a+a+b8b8b>c+d+f+<f+8f+4f+f+g+8g+4g+g+a+8a+4a+a+b8b8b>c+d+f+l8<bbbbeeeea>e<a>e<a>d+4.<a>d<a>d<a>f4.<a>e<a>e<a>d+4.<a>d<a>d<a>f4.<a>e<a>e<a>d+4.<a>d<a>dl16<eeeeef+l8ba>e<a>e<a>d+4.<a>d<a>d<a>f4.<a>e<a>e<a>d+4.<d4d4c+g+O3C+O2g+f+f+4l16f+f+g+8g+4g+g+a+8a+4a+a+b8b8b>c+d+f+<f+8f+4f+f+g+8g+4g+g+a+8a+4a+a+b8b8b>c+d+f+<f+8f+4f+f+g+8g+4g+g+a+8a+4a+a+l8bbbbbbbbeeeel4ag+f+el8dd<bal4>eeag+f+el8dd<ba>e4e4a2&a.";
            _songMMLLibrary[10] = "t126l8o2>a+a+a+a+>ddddccccffffggggggggg4r4c<a+afg8r8g8O2>>dO3>cr<a+raraa>cr<a+agrg>a+aa+aa+<grg>a+aa+aa+<g8r8g8O2>>dO3>cr<a+raraa>cr<a+agrg>a+aa+aa+<grg>a+aa+aa+<a+a+a+a+>ddddccccffffggggggggg4r4c<a+afg8r8g8O2>>dO3>cr<a+raraa>cr<a+agrg>a+aa+aa+<grg>a+aa+aa+<g8r8g8O2>>dO3>cr<a+raraa>cr<a+agrg>a+aa+aa+<grg>a+aa+aa+";
            _songMMLLibrary[11] = "t140rv127o5l8d+ff+g+a+4>d+c+<a+4d+4a+g+f+fd+ff+g+a+4g+f+fd+ff+fd+dfd+ff+g+a+4>d+c+<a+4d+4a+g+f+fd+ff+g+a+4g+f+frf+rg+ra+r>c+d+<a+g+a+4g+a+>c+d+<a+g+a+4g+a+g+f+fc+d+4c+d+ff+g+a+d+4a+>c+c+d+<a+g+a+4g+a+";
            _songMMLLibrary[12] = "v127t200o5r1l16d+rdrd+rcrdrd+rfrdrgrd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdrgrd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdrgrd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdra+rd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdrgrd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdrgrd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdrgrd+rfrdrd+rcrdr<a+>rd+rdrd+rcrdrd+rfrdra+fd+c<ga+>cda+a+b+a+gfd+f>cccc<gg>d+ccc<gg>ccc<g>cccc<gg>d+ccc<gg>ccc<g>d+d+d+d+<gg>gd+d+d+<gg>d+d+c<g>d+d+d+d+<gg>gd+d+d+<gg>d+d+c<g>cccc<g+g+>fccc<g+g+>ccc<g+>cccc<g+g+>fccc<g+g+>ccc<g+>cccc<gg>d+ccc<gg>cc<gga+gg+bb+g+b>cd8g4.d+d+d+d+ccgd+d+d+ccd+d+d+cd+d+d+d+ccgd+d+d+ccd+d+d+cggggcca+gggccggd+cggggcca+gggccggd+cffffccg+fffccfffcffffccg+fffccfffcc<g+fd>cdd+<g+>c<g+fd>cdd+<g+l8brgrdrc-rcrgfffgggggfffggrg+g+gggg+g+g+g+g+gr4g+g+r<a+>>f4<a+a+>d+4f4g<a+a+a+>d+4r1r1r1.r<c16r.d+l1.rrrrr4l8gb+g4fga+f4gfgcgl4fgf8d+d+d+.l1rrrrl4fgb+8g+a+.g+b+g+8a+.g+a+g8g+.gg+d+8d+.a+c.<a+.g+b+.a+.g+>d,o5r1l16ccfcgcd+cfcgcg+cfca+cgcg+cfcgcd+cf<a+>d<a+>ccfcgcd+cfcgcg+cfca+cgcg+cfcgcd+cfcdcc<a+>f<a+>g<a+>d+<a+>f<a+>g<a+>g+<a+>f<a+>a+<a+>g<a+>g+<a+>f<a+>g<a+>d+<a+>f<a+>d<a+>c<g+>f<g+>g<g+>d+<g+>f<g+>g<g+>g+<g+>f<g+>g<g>g<g>g+<g>f<g>g<g>d+<g>f<g>d<g>ccfcgcd+cfcgcg+cfca+cgcg+cfcgcd+cf<a+>d<a+>ccfcgcd+cfcgcg+cfca+cgcg+cfcgcd+cfcdcc<g+>f<g+>g<g+>d+<g+>f<g+>g<g+>g+<g+>f<g+>a+<g+>g<g+>g+<g+>f<g+>g<g+>d+<g+>f<g+>d<g+>c<g>f<g>g<g>d+<g>f<g>g<g>g+<g>f<g>gg+gd+<a+>dd+fg>dd+d<a+g+gg+>d+d+d+d+cccd+d+d+ccd+d+<g>cd+d+d+d+cccd+d+d+ccd+d+<g>cggggccd+gggccgg<g>cggggccd+gggccgg<g>cffffcccfffccff<g+>cffffcccfffccff<g+>cd+d+d+d+cccd+d+d+ccd+d+ccdc-cdd+cdd+f8c-4.ggggd+d+d+gggd+d+ggcd+ggggd+d+d+gggd+d+ggcd+a+a+a+a+d+d+ga+a+a+d+d+a+a+cd+a+a+a+a+d+d+ga+a+a+d+d+a+a+cd+g+g+g+g+fffg+g+g+ffg+g+cfg+g+g+g+fffg+g+g+ffg+g+cfd+c<g+f>d+fccd+c<g+f>d+fccl8d<dbc-g<g>d<dg>g>ggg4fd+f4g2fd+f4gf4d+4c2&c<gg>f4f4<<a+>a+>f4<<a+a+a+a+a+>>f4.<<a+a+>>f4gf4d+4cl16<cd+g>cd+c<gfd+cga+b+a+ga+b+4l8ggg4fd+f4d+g4.cl16<a+>rl4fgf8d+cd+.rfff8f.d+fg8f.d+fg8f.d+c8l16cr1rl8b+gb+4a+d+fa+4d+a+b+d+d+l4g+a+g+8gcg.rff.f.d+fg8f.d+8d+8fg8fd+c.rg+a+g+8b+g.fg+b+8g.fga+8f.d+fg8c.gg+b+8g.fg+b+8g.f,o5l1.rrrrrrrrrrrrrrrrr2r8>d+4.rrrrrrrrrrrrrrrrrrrrrrrrl4c<g+8a+.g+b+g+8a+.g+>d1,r1o3l4cr1.rcr1.r<a+1&a+1g+r2.g1>cr1.rcr1.r<g+1&g+1gr2.l8gr>dr<gr4.l16>crb+r8.grcrb+r8.grcrb+r8.grcrb+r8.gr<g+>rg+r8.d+r<g+>rg+r8.d+r<g+>rg+r8.d+r<g+>rg+r8.d+r<fr>fr8.cr<fr>fr8.cr<fr>fr8.cr<fr>fr8.cr<g+>rg+r8.d+r<g+>rg+r<g+ra+rg+rgrfrd+rgrfrd+rdrcr>b+r8.grcrb+r8.grcrb+r8.grcrb+r8.gr<g+>rg+r8.d+r<g+>rg+r8.d+r<g+>rg+r8.d+r<g+>rg+r8.d+r<fr>fr8.cr<fr>fr8.cr<fr>fr8.cr<fr>fr8.cr<g+rg+ra+rg+rg+rfrg+rfr>drb+r<brarbr>ar<gr>drl8g>>d+cccd+cccd+cd+d+d+cc<g+>d+ccd+cd+cd+d+d+d+d+d+cd+n22dddfddddddfddddo2a+ra+rn34a+ra+>c4r2.l16>crcr8.<gr>crcr8.<gr>crcr8.<grcrb+r8.a+rd+rg+r8.n39rg+rn39r8.d+r<g+r>>d+r8.d+r<<g+r>g+r8.n39r8.a+r8.fra+r4r>fr8.<a+r8.fra+r4r>frcr8.grcr<cr8.>gr<gr>cr4ra+rb+r<grd+rn22rcr8.crgr>crcrcr<gr>a+rcrcr<gr>crcr<crgr<g+>rg+r8.b+rg+rg+r<g+>rd+r<g+>rg+r8.b+rg+rg+ra+rb+ra+ra+r8.fra+r8.a+ra+ra+ra+r8.fra+ra+ra+ra+r>crcr8.<grcr>gr8.<gl8r.>crcrcr<a+l16crcr<fr>crfrcrcrfrcrcr<fr>crfrcrcrfrcgggggcgggggcgggcgggggcgggggcgggd+rd+rd+r<g+>rd+rd+r<g+>rd+rd+rd+rd+r<g+>rd+rd+r<g+>rd+r<a+4l8>a+a+a+a+a+a+,r1l4o5<cr1.rcr1.r<a+1&a+1g+r2.g1>cr1.rcr1.r<g+1&g+1gr2.l8gr<gr>gr2.l16>d+r4.rd+r4.rd+r4.rd+r4.rd+r4.rd+r4.rd+r4.rd+r4.rcr4.rcr4.rcr4.rcr4.rd+r1.r4.rgr4.rgr4.rgr4.rgr4.rd+r4.rd+r4.rd+r4.rd+r4.rcr4.rcr4.rcr4.rcr4.r<g+r4.rfr4.rar2rc8l1.rr4.g+8rr4.a+8rr4.l8a+ra+rfa+ra+>c4r1rl16gr4.rgr4.rgr2rd+r4r<g+>r4.rd+r4.r<g+r4.rg+r2.r8.a+r2.r8.a+r1r1r4.r>d+r4.rgr4.rd+r4.rd+r4.r<g+>r4.rcr4.r<g+r4.rg+r4.r>dr2.r8.dr2.r8.gr4.rgl8r.<grgrgrfl16b+r4ra+rcr8.g+rcrb+r4ra+rcr8.g+rcra+r4rb+r4ra+r8.a+r4rb+r4ra+r8.b+r4ra+r4rg+r8.b+r4ra+r4rg+l8r.a+4ffffff,r1o3l4gr1.l16<a+>fa+<a+>g4l1r.r4f&fd+4r2.dg4r.l16<a+>fa+<a+>g4l1r.r4d+&d+d4r2.l8drgrdr4.l16b+g>d+<gcgb+gb+g>d+<gcgb+gb+g>d+<gcgb+gb+g>d+<gcgb+gg+d+>d+<d+<g+>d+g+d+g+d+>d+<d+<g+>d+g+d+g+d+>d+<d+<g+>d+g+d+g+d+>d+<d+<g+>d+g+d+fcb+c<f>cfcfcb+c<f>cfcfcb+c<f>cfcfcb+c<f>cfcg+d+>d+<d+<g+>d+g+d+g+d+>d+<d+g+d+a+d+g+dgdfdd+dgc-fc-d+c-dc-cg>g<gcgb+gb+g>g<gcgb+gb+g>g<gcgb+gb+g>g<gcgb+gg+d+>d+<d+<g+>d+g+d+g+d+>d+<d+<g+>d+g+d+g+d+>d+<d+<g+>d+g+d+g+d+>d+<d+<g+>d+g+d+fcb+c<f>cfcfcb+c<f>cfcfcb+c<f>cfcfcb+c<f>cfcg+d+g+d+a+d+d+d+g+cfcg+ccc>d<gcgbgggbd<a>dgd<g>dl8>c>cd+d+d+cd+d+d+cd+cccd+d+<<g+>>cd+d+cd+cd+ccccccd+c<<f>>fffdffffffdffff<<f<a+>f<a+a+>f<a+>fg4r2.l16cg>g<gcgb+gcg>g<gcgb+gcg>g<gcgb+gb+g>g<gcg>f<fg+d+>d+<d+<g+>d+d+d+<g+>d+g+d+<g+>d+g+d+g+d+g+d+<g+>d+d+d+g+d+>d+<d+<g+>d+d+d+<a+>f>f<f<a+>f<a+>f>f<f<a+>f<a+>fff<a+>f>f<f<a+>f<a+>f>f<f<a+>f<a+>fffcgb+gb+gggb+gb+gb+gb+gcg>cgcg<a+>d+cd+gcd+<ga+d+b+gb+gb+gb+g>f<g>d+<g>f<gb+gb+g>g<g>f<gb+g>f<g>g<gb+gb+gg+d+>d+<d+a+d+d+d+>d+<d+b+d+a+d+g+d+g+d+>d+<d+a+d+d+d+>d+<d+b+d+<g+>d+d+d+<a+>f>d<f<a+>fa+f>g+<fa+f>g+<f>a+<f<a+>f>d<f<a+>fa+f>g+<f>g<f>g+<f>a+<fcg>g<gcgb+gb+gb+gcgb+gl8ccrccc<a+a+l16>fcfcccfc<f>cfcfc<f>cfcfcccfc<f>cfcfc<f>cg>d+cd+<a+>d+<g>d+<a+>d+cd+<g>d+cd+<g>d+cd+<a+>d+<g>d+<a+>d+cd+<g>d+cd+<g+d+g+d+g+d+d+d+g+d+g+d+d+d+g+d+g+d+g+d+g+d+d+d+g+d+g+d+d+d+g+d+f4l8<a+a+";
            _songMMLLibrary[13] = "t76>a16b16a16g16d16e16g8.e8b8e8.a16b16a16g16d16e16g8.e16b8a4a16b16a16g16d16e16g8.e8g16>d16<b8.g16a8b2.r16a16b16a16g16d16e16g8.e8b8e8.a16b16a16g16d16e16g8.e16b8a4a16b16a16g16d16e16g8.e8g16>d16<b8.g16a8b2&b16r8b16b16a16b16a16g16g8g16g16g16e16g4.a16b16a16g16a8a16a16b8a16g4&g16a16b16a16g16a16g16e16e16e16d16g4.r16<d8g8.f+8.d16r16f+8.>a16b16b8a16g16g8g16g16g16e16g4.a16b16a16g16a8a16g16b16a16a16g4&g16a16b16a16g16a16g16e16e16e16d16g2&g16r2r8.a16g16b16a16a16g16<g16g8.b16b16a16b16e4b16b16b16b8f+8.f+16f+16d16b16f+4b16b16b16b16g16g8.b16b16a16b16e4g16g16e16g16g16f+8.f+16f+16d16f+8g8g8f+16d16<a16>b16b16b16b16b16a16g16g16g4b16b8b8a8a16a16g16f+8f+8.d16b16a8g8.e16e16b8a16g8.e4>f+16d16g16f+16d16<a16>g16f+16d16<a16>g16f+16d16<a16g16f+16e16d16b16b16b16b16b16a8g16g4b16b8b8a8a16a16g16f+8f+4b16b16b16b16b8.g8g16a16g8e4&e16>f+16d16g16f+16d16<a16>g16f+16d16<a16>g16f+16<b16b16b16b16a16g16g8g16g16g16e16g4.a16b16a16g16a8a16a16b8a16a16g4a16b16a16g16a16g16e16e16e16d16g4.g16g8g8f+8.f+16f+8f+8g16a16b16b8a16g16g8g16g16g16e16g4.a16b16a16g16a8a16a16b8a16a16g4a16b16a16g16a16g16e16e16e16d16g4.g16g8g8f+8.f+16f+8f+8g8g8f+8.b16b16b16b16b16a16g16g16g4b16b8b8a8a8g16f+16f+16f+8.d16a16g8g8.e16e16b8a16g8.e4>f+16d16g16f+16d16g8f+16d16g8f+16d16<a16g16f+16>d16d16d8d16d16d16<b16b16b4e16>e16d16d16d8<a8.a16g16a16b8b8b16b16a16b8b8e16e16b8a16g8.e4>f+16d16g16f+16d16g8f+16d16<a16g16f+16b16b16b16b16a16g16g8g16g16g16e16g4.a16b16a16g16a8a16a16b8a16a16g4a16b16a16g16a16g16e16e16e16d16g4.g16g8g8f+8.f+16f+8f+8g16>d16d16d16c8<b16>c8c16c16c16<b16>c16<b4&b16>d16d16d16d16d8<a16a16>c16c16c16<b4&b16a16b16a16g16a16g16e16e16e16g16b16a4&a16g16g8g8f+8.f+16f+8f+8.a16a16b16a16>c8<b4&b16a16b16>c4.c16<f+16f+4&f+16g16f+16d16<a16>g16f+16b16a16b16a16b16b16b4&b16a16b16>c4.c16<f+16f+16e16d8.g16f+16d16<a16>d16f+16>d16d16d16d16e16d8<b4.&b16>c4.<b16>c16d2e8d4<b16a16>e4.f+16g16e16f+8g16f+16g16a16>c16d8.<g16f+16d16<b16g16f+16d16>d16d16d16d16c16<b16>c8c16c16c16<b16>d4.d16d16d16d16d8<a16a16>c8<b16b4&b16>d16d16d16d16d16c16c16c16c16<b16>d16<b4&b16g16g8g8f+8.f+16f+8f+8g16>d16d16d16d8d16e16d16d16d16d16c16<b4.>d16d16d16d16d8<a16a16>c8c16<b4&b16>d16d16d16d16d16c16c16c16c16<b16>d16<b4&b16g16g8g8f+8.f+16f+8f+8g16b16b16b16b16a16g16g8g16g16g16e16g4.a16b16a16g16a8a16a16b8a16a16g4a16b16a16g16a16g16e16e16e16d16g2&g16r2r8.>a16b16b8a16g16g8g16g16g16e16g4.a16b16a16g16a8a16g16b16a16a16g4&g16a16b16a16g16a16g16e16e16e16d16g2&g16r2r16d16d8.b16a8.a16g16e1&e2&e16";
            _songMMLLibrary[14] = "t100r4v127l8r>ggfed4.rddefe4.cl4cdedcl8<bag4.rggfed4.rddefe4.cc4def4e4d4cc-c2.,o5l8rv100gb+2.rdg2.rel4a.e.rc2<gl8rgb+2.rdg2.rea2.rf4.g2,o5v127l1c<gafc<gaf2g2";
            _songMMLLibrary[15] = "MML@t135v127l2o5g+af+g+g+aed+l8g+eg+>c+<a2f+d+f+bg+4f+er4ec+f+4c+4r4f+eg+4f+4g+eg+>c+<a4.af+d+f+bg+4f+erc+ec+f+4c+4f+f+f+eg+4f+eeec+c-ee<bb";
            _songMMLLibrary[16] = "t167r4<f4f4g4g+4>d+4c4.c8<a+4g+4a+4g+4a+4>c4<a+4g+8f2&f8f4g4g+4>d+4c2<a+4g+4a+4g+8a+4a+8>c4<a+4g+8f1&f1&f1&f1&f4.f4f4g4g+4>d+4c2<a+4g+4a+4g+4a+4>c4<a+4g+8f2&f8,o2f1&f1&f1&f1f2f4f4g+4g+4g+4g+4>d+2d+4d+4<a+4a+4a+4g+4f2f4f4g+4g+4g+4g+4>d+2d+4d+4<a+4a+4a+4g+4f2f4f4g+4g+4g+4g+4>d+2d+4d+4<a+4a+4a+4g+4f2";
            _songMMLLibrary[17] = "MML@t125v120l8>ab-bb+rb+bb-ab-bb+rb+bb-ab->cd-defdgfecr<<cc4o5ab-bb+rb+b>cdefaraaab-ab-bb+gecffefro4defa4.afafed2rdfa>d4.dcdc<ga2r4e4f4.fefadgg16g16f#gf#gf#gf#gf#gab-bb+aa16a16fffo7ff4l16";
            _songMMLLibrary[18] = "v100t65l16>ed+ed+ec-dc<a8rceab8reg+bb+8re>ed+ed+ec-dc<a8rceab8rdb+ba8rb>cde8.<g>fed8.<f>edc8.<e>dc<br8r>er8r>er8<d+er8d+ed+ed+ec-dc<a8rceab8reg+bb+8re>ed+ed+ec-dc<a8rceab8rdb+ba8;"; // ,r2l16<<a>ear8&r16<e>eg+r8&r16<a>ear2r<a>ear8&r16<e>eg+r8&r16<a>ear8&r16cgb+r8&r16<g>gbr8&r16<a>ear8&r16<e>e>eere>eerd+er8d+er2r<<<a>ear8&r16<e>eg+r8&r16<a>ear2r<a>ear8&r16<e>eg+r8&r16<a8";
            _songMMLLibrary[19] = "v100t60o4<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<a8>c+8e8<a8>c+8e8<a8>d8f+8<a8>d8f+8<g+8>c8f+8<g+8>c+8e8<g+8>c+8d+8<f+8>c8d+8<c+8g+8>c+8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>d+8f+8<g+8>d+8f+8<g+8>d+8f+8<g+8>d+8f+8<g+8>c+8e8<g+8>c+8e8<a8>c+8f+8<a8>c+8f+8<g+8b8>e8<g+8b8>e8<a8b8>d+8<a8b8>d+8<g+8b8>e8<b8>e8g+8<b8>e8g+8<b8>e8g+8<b8>f+8a8<b8>f+8a8<b8>f+8a8<b8>f+8a8<b8>e8g+8<b8>e8g+8c8f+8g+8c+8e8g+8d+8f+8g+8d+8f+8g+8e8g+8>c+8<e8g+8>c+8<d8f+8a8d8f+8a8c8f+8g+8c8f+8g+8c+8e8g+8c+8e8g+8c+8f8g+8c+8f8g+8c+8f+8a8c+8f+8a8c+8f+8a8c+8f+8a8c+8f8g+8c+8f8g+8c+8f8g+8c+8f8g+8c+8f+8a8c+8f+8a8c+8f+8a8c+8f+8a8c+8f8g+8c+8o4f8g+8c+8f+8a8c+8f+8a8<b8>f+8a8<b8>f+8a8<b8>f+8a8<b8>e8g+8<a8>e8g+8<a8>d+8f+8<g+8>d+8f+8<g+8>c+8e8<f+8>c+8d+8<f+8>c+8d+8<g+8>c+8d+8<f+8>c+8d+8<g+8>c+8e8<g+8>c+8e8<g+8>c8d+8<f+8>c8d+8<c+8g+8>c+8<g+8>c+8e8<g+8>c+8e8<g+8>c+8e8<g+8>d+8f+8<g+8>d+8f+8<g+8>d+8f+8<g+8>d+8f+8<g+8>e8c+8g+8e8>c+8<g+8>e8c+8g+8e8c+8c8d+8<a8>c8<f+8a8d+8f+8<a8>c8<g+8f+8>c+8e8c+8g+8e8>c+8<g+8>e8c+8g+8e8c+8c8d+8<a8>c8<f+8a8d+8f+8<a8>c8<g+8f+8>c+8<g+8>c+8e8c+8<g+8c+8e8g+8>c+8<g+8e8<g+8>c+8e8g+8e8c+8<g+8>c+8<g+8e8g+8e8c+2.>>c+2&c+8c+2&c+8c+2&c+8,t60o4r1r1r1r1r1r1<c+1&c+8>g+4&g+16g+16g+1&g+8g+4&g+16g+16g+2.a2.g+2.f+4.b4.e1&e8b4&b16b16b1&b8b4&b16b16b2.>c4.c+4.d+2.e2.d2.c2.c+1&c+8c+4.d1&d8c4.c+1&c+8c+4.d1&d8c4.c+2o5r4c+2.<b1&b8b4.a4.a4.g+4.g+4.f+2.g+4.a4.g+2.g+2.<e1&e8<g+4&g+16g+16g+1&g+8g+4&g+16g+16g+1&g+8g+4&g+16g+16g+1&g+8g+4&g+16g+16g+1&g+8g+4&g+16g+16g+1&g+8g+4&g+16g+16g+1&g+1&g+1&g+2.>g+2&g+8g+2g+2,t60o4<c+1&c+2<b1&b2a2.f+2.g+2.g+2.c+1&c+2>c1&c2c+2.<f+2.b2.b2.>e1&e2d+1&d+2e2.d+4.c+4.c2.c+2.<f+2.g+2.c+1&c+2.&c+8f+4.a4.f+4.c+1&c+2.&c+8f+4.a4.f+4.c+2o2t60r4f+2.>d+1&d+8e4.c+4.d+4.c4.c+4.<a2.g+4.f+4.g+2.g+2.c+1&c+2c1&c2c+1&c+2c1&c2c+1&c+2c1&c2c+1;";
            _songMMLLibrary[20] = "MML@r2l8f+g+a+b>c+.c+.c+4c+d+c+2&c+c+d+c+<ba+.a+.a+.&a+32a+32ba+f+2&f+f+g+a+b>c+.c+.c+4c+4d+4c+c+2&c+<b4a+a+g+g+16.g+32a+g+f+1&f+1&f+2;";
            _songMMLLibrary[21] = "t120g+l8ag+f+e4rg+4abg+e4rf+4g+f+d+c-4f+l16g+rf+rerc+8r4<g+rg+r>c+8rc-c+rd+re8r4.e8rd+erf+rg+8r2f+8.rl8f+d+c-4d+4ef+ed+c+r4r8g+.r16g+b>c+.r16<g+b>c+l16erc+r<brg+8.r4rf+8.rf+rg+rb8.rg+rerf+8g+8f+rerc+4";
            _songMMLLibrary[22] = "t90r4v127l8r>ggfed4.rddefe4.cl4cdedcl8<bag4.rggfed4.rddefe4.cc4def4e4d4cc-c2.,o5l8rv100gb+2.rdg2.rel4a.e.rc2<gl8rgb+2.rdg2.rea2.rf4.g2,o5v127l1c<gafc<gaf2g2";
            _songMMLLibrary[23] = "t110v127l8o4f#eereeeed#d#eero5c#o4bg#f#eereeeec#c#o3bbro5c#o4bg#f#eereeeed#d#eero5c#o4bg#f#eereeeec#c#o3bbro5c#o4bg#f#eereeeed#d#eero5c#o4bg#f#eereeeec#c#o3bbro5c#o4bg#f#eereeeed#d#eer";
            _songMMLLibrary[24] = "t115o3a+1f8g2&g8a+8.>d16c4<a+4g4f4g4f4c8<a+2.&a+8>a+1f8g2&g8a+8.>d16c4<a+4g4f4g4f4g8a+2.&a+8>c2.<a+8>c8d4c4<a+4g4>c2.&c8<a+8>c2.&c8<a+8>c16d8d8.c8<a+8g8g16f8.g1";
            _songMMLLibrary[25] = "T110V110L4rr8e16d16e<a.>rf16e16f8e8d.rf16e16f<a.>rd16c16d8c8<b8>d8cr8e16d16e<a.>rf16e16f8e8d.rf16e16f<a.>rd16c16d8c8<b8>d8cr8<b16>c16dr8c16d16e8d8c8<b8a>fe2rf16e16d8e2.";
            _songMMLLibrary[26] = "t126L8d+4d+4c4d+d+4.d+d4.d+d+4.d+4c4d+f4.d+d4cd+d+cd+4.d+cd+f4.d+d4.d+4cd+4.c4d+d+fd+4d4.d+d+c4d+4cd+4c2.&cd+d+c4d+d+cd+4f4d+d2d+d+8c4d+d+cd+4c2.&cd+d+c4d+4cd+4f4d+d";
            _songMMLLibrary[27] = "t120v127l8ef+rgrarbr>er<b2&ba4gergra4&af+ed2ef+rgrarbr>er<b2&bagrergra2&a4f+16e16d4e4f+grarbr>er<bb&br4a4gergra4&af+ed2ef+rgrarbr>er<brarargrergra&a1";
            _songMMLLibrary[28] = "g8.g16a4g4>c4<b2g8.g16a4g4>d4c2<g8.g16>g4e4c4<b4a2>f8.f16e4c4d4c1";
            _songMMLLibrary[29] = "MML@r2r8b8>e8.g16f+8e4b8a4.f+4.e8.g16f+8d4f8<b2&b8b8>e8.g16f+8e4b8>d4c+8c4<g+8>c8.<b16a+8f+4g8e2&e8g8b4g8b4g8>c4<b8a+4f+8g8.b16a+8<a+4b8>b2&b8g8b4g8b4g8>d4c+8c4<g+8>c8.<b16a+8f+4g8e2&e8;";
            _songMMLLibrary[30] = "T110o6l8v127+a#dga#>c<faa#4ga#>dd#<g>dc<a#dga#>c<faa#4ga#>dd#<g>dc<a#dga#>c<faa#4ga#>dd#<g>dc<a#dga#acfg4";
            _songMMLLibrary[31] = "V127T90c8d8d+4.d8d+4g4d2.<g4>c4.<a+8>c4d+4<a+2.g4g+4.g8g+8>d+4.<g2.>d+4d4.<a8a4>d4d2.c8d8d+4.d8d+4g4d2.<g4>c4.<a+8>c4d+4<a+2.g4g+4>d+8d4.d+4f4g8d+2&d+8d+8d8c4d4<b4>c2.";
            _songMMLLibrary[32] = "t92l8f2&fccfe-16d-16e-&e-4&e-2f2&fd-d-fe16d16e&e4&e2f4c4&cfl16fgab->c2<r2l8f4c4&cfl16fgab->c2<r2,l4o3f>cf2<e-b->e-2<d-a->d-2<cg>c2l1rl16r8f8fgab->c2<r1r8o4f8fgab->c2";
            _songMMLLibrary[33] = "T115L8V127fgg#2R4d#d#a#2R4g#4fff4fg4g#2R4fgg#2R4d#>c<a#2R4g#a#b#4b#4>c#4<b#a#a#g#2R4>d#4.<b#4.a#2R4g#4g#4>d#4.<b#4.g#2.R4g#g#g4.d#4.d#2.R8.cc#4c#cc#cc#c#c<g#1";
            _songMMLLibrary[34] = "MML@t130rl8cc>ccl4<a+agfc2c8.c16g.f8g.f8ec2d>d8d8c<a+ag2.>e8.d16cc8.<a+16aa8.g16f2.c>c8c8<a+agfc2c8.c16g.f8g.f8ec2d>d8d8c<a+ag2.l8.>ed16c4c<a+16a4ag16f2.;";
            _songMMLLibrary[35] = "t106ebbgf#f#f#f#8g8ebbgf#f#f#f#8g8ebbgf#f#f#f#8g8ebbgf#.8b8b>co3l16eeeee4.<b8>e4ddddd4.<a8>d4ccccc4.<g8>c4ddddd4.<a8>d4eeeee4.<b8>e4ddddd4.<a8>d4ccccc4.<g8>c4";
            _songMMLLibrary[36] = "t80#L16dd>dr<arrg+rgrffdfgcc>dr<arrg#r-grffdfg<b-b->>dr<arrg#rgrffdfg<b#b#>>dr<arrg#rgrffdfgdd>dr<arrg#rgrffdfgcc>dr<arrg#rgrffdfg";
            _songMMLLibrary[37] = "t115l8<f+af+e4a4.>dedl4c+<a.gab>c+t229d2<c+d8e.d2.l8&df+f+f+f+4dl4e.df+ed2c+d8e.d2d8e8f+8aaf+ed8f+e2.d8d8df+a8ba.de8e8eeed8f+e2&e8d8ef+l8daaal4ba.deeef+ba8af+8gd2c+d8e.d2d8e8f+d8gf+e.df+2d1;";
            _songMMLLibrary[38] = "MML@T80r2.r8c+8c+8d+8f8c+8d+8d+4c+8g+8g+16g+8a+8.g+4.c+8f8f8f8g+8g+16f8d+8.c+8f8f16f8f+8.c+4.<a+16>c16c+8c+8c+8f8d+16c+8c+8.c+16d+16f8f16f8f+8.d+4.f+8g+8g+16g+8.g+8g+8f8d+8c+8d+8c+8d+16f8d+4;";
            _songMMLLibrary[39] = "t90v127o6f+8g+8d+16d+8<b16>d16c+16<b8b8>c+8d8d16c+16<b16>c+16d+16f+16g+16d+16f+16c+16d+16<b16>c+16<b16>d+8f+8g+16d+16f+16c+16d+16<b16>d16d+16d16c+16<b16>c+16d8<b16>c+16d+16f+16c+16d+16c+16<b16>c+8<b8";
            _songMMLLibrary[40] = "r1eefggfedccdee.d8d2eefggfedccded.c8c2ddecde8f8ecde8f8edcd<g>e2efggfedccded.c8c2;";
            _songMMLLibrary[41] = "t100v127l16fcl8.ddc8fl16fr8fcl8.ddc8fl16ar8fcd8.d8dc8f8.frfg8f8.frfd8ar8.r8.e64f8&f32.frfgra8.fr8cdf8.frfgra8.fr8drfrdfr8drfrdfr8fra+arf8.grf4";
            _songMMLLibrary[42] = "MML@t150>f8g8a4>c2.<f4e2>d4.c16d16c2.<a8a+4a4g4a4&a32<f32a+32>d32f1<e2>c2d2.&d8d4d8d2.<f8e4f4g4<a4.&a16";
            _songMMLLibrary[43] = "T55l8v80>at70g#ag#at55ead2<a16>c#16t70ag#ag#at55ead2l16<a>c#t70a8g#a8<a>g#a8<a>ea8<a>d<al8>c#dec#<b4.";
            //populate song name array on runtime (will be displayed on the keys, keep it short)
            _songNameLibrary[0] = "Wish you merry xmas (easy)";
            _songNameLibrary[1] = "12 Days of Christmas";
            _songNameLibrary[2] = "I wont be home for xmas";
            _songNameLibrary[3] = "Jingle Bells";
            _songNameLibrary[4] = "O Christmas Tree";
            _songNameLibrary[5] = "Last Christmas";
            _songNameLibrary[6] = "White Christmas";
            _songNameLibrary[7] = "Still Alive (easy)";
            _songNameLibrary[8] = "Faded";
            _songNameLibrary[9] = "Want You Gone";
            _songNameLibrary[10] = "Coffin Dance Song";
            _songNameLibrary[11] = "Bad Apple (easy)";
            _songNameLibrary[12] = "Through The Fire And Flames";
            _songNameLibrary[13] = "Let Her Go";
            _songNameLibrary[14] = "Wherever you go";
            _songNameLibrary[15] = "Barbie Girl";
            _songNameLibrary[16] = "Boulevard of Broken Dreams";
            _songNameLibrary[17] = "Math Class";
            _songNameLibrary[18] = "Für Elise";
            _songNameLibrary[19] = "Moonlight Sonata";
            _songNameLibrary[20] = "Caramelldansen";
            _songNameLibrary[21] = "Butterfly Samurai";
            _songNameLibrary[22] = "Wherever you go";
            _songNameLibrary[23] = "Levels";
            _songNameLibrary[24] = "Hey Brother";
            _songNameLibrary[25] = "Final Countdown";
            _songNameLibrary[26] = "Sweet Dreams";
            _songNameLibrary[27] = "Feel Good Inc.";
            _songNameLibrary[28] = "Happy Birthday";
            _songNameLibrary[29] = "Harry Potter Theme";
            _songNameLibrary[30] = "I'm blue";
            _songNameLibrary[31] = "Castle in the Sky";
            _songNameLibrary[32] = "Legend of Zelda";
            _songNameLibrary[33] = "Let it go";
            _songNameLibrary[34] = "Let it snow";
            _songNameLibrary[35] = "In the end";
            _songNameLibrary[36] = "Megalovania Basic";
            _songNameLibrary[37] = "MLP Intro";
            _songNameLibrary[38] = "MLP Winter Wrapup";
            _songNameLibrary[39] = "Nyan Cat";
            _songNameLibrary[40] = "Ode to Joy";
            _songNameLibrary[41] = "Renai Circulation";
            _songNameLibrary[42] = "SAO Opening";
            _songNameLibrary[43] = "River Flows in You (Yiruma)";
        }
        #endregion SongLibrary
    }
}