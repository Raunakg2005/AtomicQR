using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Text;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Toggle rememberMeToggle;

    public RawImage successImage;
    public RawImage errorImage;  

    private string correctUsername = "RKG";
    private string correctPasswordHash = "2e1097c3411519a3733a0b48a533fd7428d2fba77e46bf317a063d223c9012f7"; // hash of "password"

    void Start()
    {
        successImage.gameObject.SetActive(false);
        errorImage.gameObject.SetActive(false);

        if (PlayerPrefs.GetInt("rememberMe", 0) == 1)
        {
            string savedUsername = PlayerPrefs.GetString("username", "");
            string savedHash = PlayerPrefs.GetString("passwordHash", "");

            if (savedUsername == correctUsername && savedHash == correctPasswordHash)
            {
                Debug.Log("✅ Auto login successful!");
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }
    }

    public void OnLogin()
    {
        successImage.gameObject.SetActive(false);
        errorImage.gameObject.SetActive(false);

        StartCoroutine(HandleLogin());
    }

    private IEnumerator HandleLogin()
    {
        yield return null;

        string enteredUsername = usernameInput.text.Trim();
        string enteredPassword = passwordInput.text;
        string enteredHash = ComputeSha256Hash(enteredPassword);

        if (enteredUsername == correctUsername && enteredHash == correctPasswordHash)
        {
            successImage.gameObject.SetActive(true);
            errorImage.gameObject.SetActive(false);

            if (rememberMeToggle != null && rememberMeToggle.isOn)
            {
                PlayerPrefs.SetInt("rememberMe", 1);
                PlayerPrefs.SetString("username", enteredUsername);
                PlayerPrefs.SetString("passwordHash", enteredHash);
            }
            else
            {
                PlayerPrefs.DeleteKey("rememberMe");
                PlayerPrefs.DeleteKey("username");
                PlayerPrefs.DeleteKey("passwordHash");
            }

            yield return new WaitForSeconds(1f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("App");
        }
        else
        {
            errorImage.gameObject.SetActive(true);
            successImage.gameObject.SetActive(false);
        }
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
