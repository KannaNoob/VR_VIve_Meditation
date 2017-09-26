using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Microphone_Input : MonoBehaviour {


    public bool start_lotus = false;
    bool mutable = false;
    public int truecount, falsecount;
    public float time;

    private const int FREQUENCY = 48000;    // Wavelength
    private const int SAMPLECOUNT = 256;   // Sample Count.
    private const float REFVALUE = 0.1f;    // RMS value for 0 dB.
    private const float THRESHOLD = 0.02f;  // Minimum amplitude to extract pitch
    private const float ALPHA = 0.05f;      // The alpha for the low pass filter

    public GameObject resultDisplay;   // GUIText for displaying results
    public GameObject blowDisplay;     // GUIText for displaying blow or not blow.
    public int recordedLength = 2;    // How many previous frames of sound are analyzed.
    public int requiedBlowTime = 5;    // How long a blow must last to be classified as a blow (and not a sigh for instance).
    public int clamp = 160;            // Used to clamp dB
    

    private float rmsValue;            // Volume in RMS
    private float dbValue;             // Volume in DB
    public float pitchValue;          // Pitch - Hz
    public float sumPitch;
    private int blowingTime;           // How long each blow has lasted

    public float lowPassResults;      // Low Pass Filter result
    private float peakPowerForChannel; //

    private float[] samples;           // Samples
    private float[] spectrum;          // Spectrum
    private List<float> dbValues;      // Used to average recent volume.
    private List<float> pitchValues;   // Used to average recent pitch.


    public float sensitivity = 100;
    public float loudness = 0;


    AudioSource Aud;

    // Use this for initialization
    void Start () {

        samples = new float[SAMPLECOUNT];
        spectrum = new float[SAMPLECOUNT];
        dbValues = new List<float>();
        pitchValues = new List<float>();


        Aud = GetComponent<AudioSource>();
       



    }
    IEnumerator disablebreath()
    {

        yield return new WaitForSeconds(4.0f);
        blowDisplay.SetActive(true);
        resultDisplay.SetActive(false);
        start_lotus = false;
    }

    IEnumerator breathCheck()               // Bounce one Object, and Test the other
    {
        mutable = true;
        time = 0;
      
        while(time < 2.5f )
        {
            blowDisplay.transform.Translate(0,0.02f, 0);

            if (lowPassResults > -52.0f)
            {
                resultDisplay.transform.Translate(0, 0.02f, 0);

            }
            
            time += Time.deltaTime;
            yield return null;
        }
        

        blowDisplay.transform.position = new Vector3(-1.46f, 6.85f, 7.05f);
        resultDisplay.transform.position = new Vector3(-1.46f, 6.85f, 7.05f);
        mutable = false;
    }

    void StartMic()
    {
    
        Aud.clip = Microphone.Start(null, true, 2, AudioSettings.outputSampleRate);
        Aud.loop = true;
        while (!(Microphone.GetPosition(null) > 0)) { }
        Aud.Play();

    }


    float getAvgVolume()
    {
        float[] data = new float[256];
        float a = 0;
        Aud.GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / 256;

    }


   void AnalyzeSound()
    {

        // Get all of our samples from the mic.
        Aud.GetOutputData(samples, 0);

        // Sums squared samples
        float sum = 0;
        for (int i = 0; i < SAMPLECOUNT; i++)
        {
            sum += Mathf.Pow(samples[i], 2);
        }

        // RMS is the square root of the average value of the samples.
        rmsValue = Mathf.Sqrt(sum / SAMPLECOUNT);
        dbValue = 20 * Mathf.Log10(rmsValue / REFVALUE);

        // Clamp it to {clamp} min
        if (dbValue < -clamp)
        {
            dbValue = -clamp;
        }

        // Gets the sound spectrum.
        Aud.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        float maxV = 0;
        int maxN = 0;
        
        // Find the highest sample.
        for (int i = 0; i < SAMPLECOUNT; i++)
        {
            if (spectrum[i] > maxV && spectrum[i] > THRESHOLD){
            maxV = spectrum[i];
            maxN = i; // maxN is the index of max
        }
    }

    // Pass the index to a float variable
    float freqN = maxN;
 
    // Interpolate index using neighbours
    if (maxN > 0 && maxN<SAMPLECOUNT - 1) {
            float dL = spectrum[maxN - 1] / spectrum[maxN];
            float dR = spectrum[maxN + 1] / spectrum[maxN];
            freqN += 0.5f * (dR* dR - dL* dL);
        }

    // Convert index to frequency
        pitchValue = freqN* 24000 / SAMPLECOUNT;

    }

    private void DeriveBlow()
    {

        UpdateRecords(dbValue, dbValues);
        UpdateRecords(pitchValue, pitchValues);

        // Find the average pitch in our records (used to decipher against whistles, clicks, etc).
        sumPitch = 0;
        foreach (float num in pitchValues)
        {
            sumPitch += num;
        }
        sumPitch /= pitchValues.Count;

        // Run our low pass filter.
        lowPassResults = LowPassFilter(dbValue);
     //  
    }

    private void UpdateRecords(float val, List<float> record)
    {
        if (record.Count > recordedLength)
        {
            record.RemoveAt(0);
        }
        record.Add(val);
    }
    
    private float LowPassFilter(float peakVolume)
    {
        return ALPHA * peakVolume + (1.0f - ALPHA) * lowPassResults;
    }


    // Update is called once per frame
    void Update () {

        if(!Aud.isPlaying)
        {
            StartMic();

        }


        loudness = getAvgVolume() * sensitivity;

      
        AnalyzeSound();
        DeriveBlow();

        if (!mutable)
            StartCoroutine(breathCheck());
        
    }
}
