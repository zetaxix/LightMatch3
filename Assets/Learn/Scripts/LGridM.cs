using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LGridM : MonoBehaviour
{
    public int width = 6;
    public int height = 6;
    public float spacing = 1.1f;

    public LTile tilePrefab;
    public Sprite[] tileSprites;

    private LTile[,] grid;

    private LTile selectedTile = null;

    public Light2D highlightLight; // UnityEngine.Rendering.Universal.Light2D

    private bool isSwapping = false;

    public TextMeshPro scoreText; // veya UnityEngine.UI.Text
    private int score = 0;

    public GameObject scorePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 0.6f, 0f);

    public List<AudioClip> matchSounds;

    private Sprite[] shuffledSprites;

    private void Start()
    {
        StartCoroutine(DelayedGenerate());
    }

    IEnumerator DelayedGenerate()
    {
        yield return null; // 1 frame bekle (opsiyonel)
        GenerateGrid();
    }

    void SpawnScorePopup(Vector3 worldPos, int amount, Color color)
    {
        GameObject popup = Instantiate(scorePopupPrefab, worldPos + popupOffset, Quaternion.identity);
        ScorePopup popupScript = popup.GetComponent<ScorePopup>();
        popupScript.Setup(amount, color);
    }

    void AddScore(int amount)
    {
        score += amount;
        scoreText.text = score.ToString();

        Animator scoreAnimtor = scoreText.GetComponent<Animator>();
        scoreAnimtor.SetTrigger("Score");
    }

    int CalculateScoreByMatchCount(int matchCount)
    {
        if (matchCount >= 5)
        {
            Handheld.Vibrate();
            return 10;
        }
        else if (matchCount == 4)
        {
            Handheld.Vibrate();
            return 5;
        }
        else if (matchCount == 3)
        {
            Handheld.Vibrate();
            return 3;
        }
        else
        {
            return 0;
        }
    }

    void PlayRandomMatchSound()
    {
        if (matchSounds == null || matchSounds.Count == 0) return;

        AudioClip clip = matchSounds[Random.Range(0, matchSounds.Count)];
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.8f);
    }

    void GenerateGrid()
    {
        grid = new LTile[width, height];

        Vector2 offset = new Vector2(width - 1, height - 1) * spacing / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = new Vector2(x * spacing, y * spacing) - offset;
                LTile tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tile.SetManager(this); // LTile GridM in seçildiğini haber veriyor.

                Sprite validSprite = GetValidSprite(x, y);
                tile.SetSprite(validSprite);

                grid[x, y] = tile;
            }
        }
    }

    Sprite GetValidSprite(int x, int y)
    {
        //sprite listesini rastgele sırala
        shuffledSprites = tileSprites.OrderBy(s => Random.value).ToArray();

        foreach (var candidate in shuffledSprites)
        {
            bool causesMatch = false;

            //Yatayda eşleşme var mı kontrol
            if (x >= 2)
            {
                Sprite left = grid[x - 1, y].GetSprite();
                Sprite left2 = grid[x - 2, y].GetSprite();

                if (left == candidate && left2 == candidate)
                    causesMatch = true;
            }

            if (y >= 2)
            {
                Sprite down = grid[x, y - 1].GetSprite();
                Sprite down2 = grid[x, y - 2].GetSprite();

                if (down == candidate && down2 == candidate)
                    causesMatch = true;
            }

            if (!causesMatch)
                return candidate;
        }

        return tileSprites[0];
    }

    public void OnTileClicked(LTile clickedTile)
    {
        if (selectedTile == null)
        {
            // İlk taşı seçtik
            selectedTile = clickedTile;
        }
        else
        {
            // İkinci taşı seçtik, şimdi ikisini karşılaştır
            if (AreNeighbors(selectedTile, clickedTile))
            {
                SwapTilesInGrid(selectedTile, clickedTile); // Henüz yazmadık
            }

            // Seçimi sıfırla
            selectedTile = null;
        }
    }

    bool AreNeighbors(LTile a, LTile b)
    {
        Vector2Int posA = GetTilePosition(a);
        Vector2Int posB = GetTilePosition(b);

        int dx = Mathf.Abs(posA.x - posB.x);
        int dy = Mathf.Abs(posA.y - posB.y);

        // Sadece yatay veya dikey komşu olabilir (çapraz olmaz!)
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    Vector2Int GetTilePosition(LTile tile)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] == tile)
                    return new Vector2Int(x, y);

        return Vector2Int.zero;
    }

    public void SwapTilesInGrid(LTile a, LTile b)
    {
        Vector2Int posA = GetTilePosition(a);
        Vector2Int posB = GetTilePosition(b);

        // Dizide yer değiştir
        grid[posA.x, posA.y] = b;
        grid[posB.x, posB.y] = a;
    }

    public void AnimateSwap(LTile a, LTile b, float duration = 0.3f)
    {
        Vector2 posA = a.transform.position;
        Vector2 posB = b.transform.position;

        a.MoveToPosition(posB, duration);
        b.MoveToPosition(posA, duration);
    }

    public void TrySwipe(LTile fromTile, Vector2Int direction)
    {
        if (isSwapping) return; // ↩ input kilidi açıkken işlem yapma
        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return;

        Vector2Int from = GetTilePosition(fromTile);
        Vector2Int to = from + direction;

        if (!IsValidPosition(to)) return;

        isSwapping = true; // 🔒 kilidi açma

        LTile tileA = grid[from.x, from.y];
        LTile tileB = grid[to.x, to.y];

        AnimateSwap(tileA, tileB);
        StartCoroutine(HandleSwapResult(tileA, tileB, from, to, 0.3f));
    }

    IEnumerator HandleSwapResult(LTile tileA, LTile tileB, Vector2Int from, Vector2Int to, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Mantıksal olarak swap'le
        SwapTilesInGrid(tileA, tileB);

        List<LTile> matchFrom = GetMatchesAt(to);
        List<LTile> matchTo = GetMatchesAt(from);

        if (matchFrom.Count >= 3 || matchTo.Count >= 3)
        {
            List<LTile> allMatches = matchFrom.Concat(matchTo).Distinct().ToList();

            int calculatedScore = CalculateScoreByMatchCount(allMatches.Count);
            AddScore(calculatedScore);

            if (allMatches.Count > 0)
            {
                PlayRandomMatchSound();

                Color matchColor = GetColorFromSpriteName(allMatches[0].GetSprite());
                StartCoroutine(FlashLight(matchColor));

                Vector3 popupPos = allMatches[0].transform.position; // eşleşmenin pozisyonu
                SpawnScorePopup(popupPos, calculatedScore, matchColor);

            }

            foreach (var tile in allMatches)
            {
                Vector2Int pos = GetTilePosition(tile);
                grid[pos.x, pos.y] = null;
                tile.DestroyWithEffect(); // Görselli silme
            }

            FillGrid();

            yield return new WaitForSeconds(0.4f);
            StartCoroutine(CheckAndHandleMatches());
        }
        else
        {
            // Eşleşme yoksa geri al (hem görsel hem mantıksal)
            AnimateSwap(tileA, tileB); // Geri döndürme animasyonu
            yield return new WaitForSeconds(delay);
            SwapTilesInGrid(tileA, tileB); // Mantıksal swap geri al
        }

        isSwapping = false; // 🔓 input tekrar aktif
    }

    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public List<LTile> GetMatchesAt(Vector2Int pos)
    {
        if (!IsValidPosition(pos) || grid[pos.x, pos.y] == null)
            return new List<LTile>();  // Boş liste döndür, işlem yapma

        Sprite target = grid[pos.x, pos.y].GetSprite();

        List<LTile> horizontal = new List<LTile> { grid[pos.x, pos.y] };

        for (int x = pos.x - 1; x >= 0; x--)
        {
            if (grid[x, pos.y].GetSprite() == target)
                horizontal.Add(grid[x, pos.y]);
            else
                break;
        }

        // Sağa doğru bak (x artıyor)
        for (int x = pos.x + 1; x < width; x++)
        {
            if (grid[x, pos.y].GetSprite() == target)
                horizontal.Add(grid[x, pos.y]);
            else
                break;
        }

        List<LTile> vertical = new List<LTile> { grid[pos.x, pos.y] };

        // Aşağı doğru bak (y azalıyor)
        for (int y = pos.y - 1; y >= 0; y--)
        {
            if (grid[pos.x, y].GetSprite() == target)
                vertical.Add(grid[pos.x, y]);
            else
                break;
        }

        // Yukarı doğru bak (y artıyor)
        for (int y = pos.y + 1; y < height; y++)
        {
            if (grid[pos.x, y].GetSprite() == target)
                vertical.Add(grid[pos.x, y]);
            else
                break;
        }

        List<LTile> matches = new List<LTile>();

        // horizontal ve vertical listelerimiz var
        if (horizontal.Count >= 3)
            matches.AddRange(horizontal);

        if (vertical.Count >= 3)
            matches.AddRange(vertical);

        // Tekrar edenleri çıkar
        matches = matches.Distinct().ToList();

        return matches;


    }

    public void FillGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    // Yukarıdan ilk dolu taşı bul
                    for (int yAbove = y + 1; yAbove < height; yAbove++)
                    {
                        if (grid[x, yAbove] != null)
                        {
                            grid[x, y] = grid[x, yAbove];
                            grid[x, yAbove] = null;

                            Vector2 newPos = GetWorldPosition(x, y);
                            grid[x, y].MoveToPosition(newPos);

                            break;
                        }
                    }

                    // Eğer yukarıda hiç taş yoksa, yeni taş üret
                    if (grid[x, y] == null)
                    {
                        Vector2 spawnPos = GetWorldPosition(x, height + 1);
                        LTile newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);

                        newTile.SetManager(this);
                        newTile.SetSprite(GetValidSprite(x, y));
                        grid[x, y] = newTile;

                        Vector2 targetPos = GetWorldPosition(x, y);
                        newTile.MoveToPosition(targetPos);
                    }
                }
            }
        }

        // Eşleşme kontrolü ve ışık efekti
        StartCoroutine(CheckCascadeMatches());
    }

    IEnumerator CheckCascadeMatches()
    {
        yield return new WaitForSeconds(0.35f); // taşlar otursun

        List<LTile> matched = new List<LTile>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] != null)
                    matched.AddRange(GetMatchesAt(new Vector2Int(x, y)));

        matched = matched.Distinct().ToList();

        if (matched.Count > 0)
        {
            // Işık yak (ilk taşın sprite isminden renk al)
            Color matchColor = GetColorFromSpriteName(matched[0].GetSprite());
            StartCoroutine(FlashLight(matchColor));

            PlayRandomMatchSound();

            int calculatedScore = CalculateScoreByMatchCount(matched.Count);
            AddScore(calculatedScore);

            Vector3 popupPos = matched[0].transform.position;
            SpawnScorePopup(popupPos, calculatedScore, matchColor);

            foreach (var tile in matched)
            {
                Vector2Int pos = GetTilePosition(tile);
                grid[pos.x, pos.y] = null;
                tile.DestroyWithEffect();
            }

            yield return new WaitForSeconds(0.3f);
            FillGrid(); // cascade devam et
        }
    }

    Vector2 GetWorldPosition(int x, int y)
    {
        Vector2 offset = new Vector2((width - 1), (height - 1)) * spacing / 2f;
        return new Vector2(x * spacing, y * spacing) - offset;
    }

    IEnumerator CheckAndHandleMatches()
    {
        bool hasMatch;

        do
        {
            hasMatch = false;
            List<LTile> totalMatches = new List<LTile>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] == null) continue;

                    List<LTile> matches = GetMatchesAt(new Vector2Int(x, y));
                    if (matches.Count >= 3)
                    {
                        hasMatch = true;
                        totalMatches.AddRange(matches);
                    }
                }
            }

            if (hasMatch)
            {
                foreach (var tile in totalMatches.Distinct())
                {
                    Vector2Int pos = GetTilePosition(tile);
                    grid[pos.x, pos.y] = null;
                    tile.DestroyWithEffect(); // Görselli silme
                }

                yield return new WaitForSeconds(0.2f);
                FillGrid();
                yield return new WaitForSeconds(0.4f); // animasyon süresi
            }

        } while (hasMatch);
    }

    public IEnumerator FlashLight(Color color, float flashTime = 0.5f)
    {
        if (highlightLight == null) yield break;

        highlightLight.color = color;
        highlightLight.intensity = 0.35f;

        yield return new WaitForSeconds(flashTime);

        float duration = 0.5f;
        float t = 0f;
        Color startColor = color;
        Color endColor = Color.black;

        while (t < duration)
        {
            t += Time.deltaTime;
            highlightLight.color = Color.Lerp(startColor, endColor, t / duration);
            yield return null;
        }

        highlightLight.intensity = 0f;
    }

    Color GetColorFromSpriteName(Sprite sprite)
    {
        string name = sprite.name.ToLower();

        if (name.Contains("red")) return Color.red;
        if (name.Contains("blue")) return Color.blue;
        if (name.Contains("green")) return Color.green;
        if (name.Contains("yellow")) return Color.yellow;
        if (name.Contains("purple")) return new Color(0.6f, 0f, 0.8f);

        return Color.white; // fallback
    }

}
