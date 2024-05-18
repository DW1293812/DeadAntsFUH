using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class Manager : MonoBehaviour
{
    public Camera camCamera;
    public int gridSizeX, gridSizeY;
    public Knoten[,] grid;
    public GameObject knotenPrefab, antPrefab, deadAntPrefab;
    public LineRenderer kantenPrefab;
    public float sensitvity = 20f;
    public bool simulate, isSimulating;
    public List<Ant> allAnts = new List<Ant>();
    public List<Ant> deadAnts = new List<Ant>();
    public float simulationSpeed = 1f;
    public int livingAntChance, deadAntChance;
    public float alpha = 0.5f;
    public float pickUpKonstant1, pickUpKonstant2;
    public float decayrate;
    public int maxSeeRange;
    bool finised = false;
    bool menu = true;
    public TMP_Text buttonText;
    public GameObject graphicBackground;
    public Material lineMat;
    public Color lineGraphic, lineSchematic;

    public UIElement[] uIElements;
    public List<GameObject> allObjects;
    bool isSchematic = false;
    bool invincible = true;
    public Ant selectedAnt = null;
    public LayerMask mask;
    public LineRenderer pathShower;
    // Start is called before the first frame update
    void Start()
    {

        ResetMe();


    }

    private void ResetMe()
    {
        buttonText.text = "START";
        simulate = true;
        gridSizeX = 20;
        gridSizeY = 20;
        deadAntChance = 0;
        livingAntChance = 20;
        simulationSpeed = 1f;
        alpha = 0.2f;
        for (int i = 0; i < allObjects.Count; i++)
        {
            Destroy(allObjects[i]);
        }
        allObjects.Clear();
        allAnts.Clear();
        deadAnts.Clear();
        uIElements[0].myValue = gridSizeX;
        uIElements[1].myValue = gridSizeY;
        uIElements[2].myValue = 25;
        uIElements[3].myValue = 25;
        uIElements[4].myValue = 0;
        uIElements[5].myValue = 0.2f;
        simulationSpeed = 1;
        for (int i = 0; i < uIElements.Length; i++)
        { uIElements[i].main.SetActive(true); }

        ChangeUiElements(true);

        InitTorus();
    
        menu = true;
    }

    public void StartPressed()
    {
        if (menu)
        StartCoroutine(StartSimulation());
        else
            ResetMe();
    }

    public IEnumerator StartSimulation()
    {
        buttonText.text = "STOP";
        for (int i = 0; i < uIElements.Length; i++)
        { if (i != 4) uIElements[i].main.SetActive(false); }
        simulate = false;
        while (isSimulating)
        {
            yield return null;
        }
        yield return null;

        for (int i = 0; i < allObjects.Count; i++)
        {
            Destroy(allObjects[i]);
        }
        allObjects.Clear();
        allAnts.Clear();
        deadAnts.Clear();
        gridSizeX = (int)uIElements[0].myValue;
        gridSizeY = (int)uIElements[1].myValue;
        livingAntChance = (int)uIElements[2].myValue;
        deadAntChance = (int)uIElements[3].myValue;
        simulationSpeed = uIElements[4].myValue * 5 + 1;
        alpha = uIElements[5].myValue;
        InitTorus();
        simulate = true;
        menu = false;
    }

    private void Update()
    {


        if (simulate && !isSimulating)
        {
            StartCoroutine(simulateStep());
        }

        if (!menu)
        {
            CamMovement();
        }

        //DebugStuff
        int count = allAnts.Count;
        for (int i = 0; i < count; i++)
        {
            if (allAnts[i].path.Count > 0)
                for (int i2 = 1; i2 < allAnts[i].path.Count; i2++)
                {
                    Debug.DrawLine(new Vector3(allAnts[i].path[i2 - 1].x, allAnts[i].path[i2 - 1].y, 1), new Vector3(allAnts[i].path[i2].x, allAnts[i].path[i2].y, 1), Color.red);
                }
        }
    }

    void CamMovement ()
    {
        if (Input.GetMouseButton(1))
        {
            Vector2 MouseMoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            camCamera.transform.Translate(MouseMoveDirection * Time.deltaTime * sensitvity);
            camCamera.transform.position = new Vector3(Mathf.Clamp(camCamera.transform.position.x, 0, gridSizeX), Mathf.Clamp(camCamera.transform.position.y, 0, gridSizeY), camCamera.transform.position.z);

        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Event currentEvent = Event.current;
            Vector2 mousePos = new Vector2();
            mousePos.x = Input.mousePosition.x;
            mousePos.y = Input.mousePosition.y;

            var position = camCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, camCamera.nearClipPlane));
          
            if (Physics.Raycast(position, Vector3.forward, out hit, 200f, mask))
            {
            
                selectedAnt = allAnts.SingleOrDefault(x => x.index.Equals(hit.transform.gameObject.GetComponent<AntPrefab>().index));
                

            } else
            {
                pathShower.positionCount = 0;
                selectedAnt = null;
            }



        }
        camCamera.orthographicSize = Mathf.Clamp(camCamera.orthographicSize - Input.GetAxisRaw("Mouse ScrollWheel") * 5f, 1, Mathf.Max(gridSizeY, gridSizeX));


        if (selectedAnt != null && selectedAnt.path != null && selectedAnt.path.Count > 0)
        {
            List<Vector3> posses = new List<Vector3>();
            posses.Add(new Vector3(selectedAnt.myGridPosition.x, selectedAnt.myGridPosition.y, -2));
            posses.Add(new Vector3(selectedAnt.nextGridPosition.x, selectedAnt.nextGridPosition.y, -2));


            for (int i = 0; i < selectedAnt.path.Count; i++)
            {
                posses.Add(new Vector3(selectedAnt.path[i].x, selectedAnt.path[i].y, -2));
            }
            pathShower.positionCount = posses.Count;
            pathShower.SetPositions(posses.ToArray());

        }

    }



    IEnumerator simulateStep()
    {
        isSimulating = true;

        for (int i = 0; i < allAnts.Count; i++)
        {
            Ant currentAnt = allAnts[i];
            currentAnt.nextGridPosition = NextGridPosition(currentAnt.myGridPosition, currentAnt);
           // currentAnt.myPrefab.transform.LookAt(grid[currentAnt.nextGridPosition.x, currentAnt.nextGridPosition.y].knoten.transform.position);


        }

        float timer = 0f;
        while (timer < 1f)
        {
            if (simulationSpeed > 20)
            {
                timer = 1f;
            }
            for (int i = 0; i < allAnts.Count; i++)
            {
                Ant currentAnt = allAnts[i];
                var rotDirection = Quaternion.LookRotation(grid[currentAnt.nextGridPosition.x, currentAnt.nextGridPosition.y].knoten.transform.position - grid[currentAnt.myGridPosition.x, currentAnt.myGridPosition.y].knoten.transform.position, Vector3.forward);
                rotDirection.x = 0f;
                rotDirection.y = 0f;
                if (currentAnt.myGridPosition != currentAnt.nextGridPosition)
                {
                    currentAnt.myPrefab.transform.rotation = Quaternion.Lerp(currentAnt.myPrefab.transform.rotation, rotDirection, Time.deltaTime * 5f);
                    
                }



                if (Vector2Int.Distance(currentAnt.myGridPosition, currentAnt.nextGridPosition) < 2)
                {
                    currentAnt.myPrefab.transform.position = Vector2.Lerp(grid[currentAnt.myGridPosition.x, currentAnt.myGridPosition.y].knoten.transform.position, grid[currentAnt.nextGridPosition.x, currentAnt.nextGridPosition.y].knoten.transform.position, timer);
                    currentAnt.anim.SetBool("Walk", true);
                }
                else
                {
                    if (!isSchematic)
                    {
                        currentAnt.anim.SetBool("Fly", true);
                        var middlePos = Vector2.Lerp(currentAnt.myGridPosition, currentAnt.nextGridPosition, 0.5f);
                        var middlePos3d = new Vector3(middlePos.x, middlePos.y, -2f);

                        if (timer < 0.5f)
                        {
                            currentAnt.myPrefab.transform.position = Vector3.Lerp(grid[currentAnt.myGridPosition.x, currentAnt.myGridPosition.y].knoten.transform.position, middlePos3d, timer * 2f);
                        }
                        else
                        {
                            currentAnt.myPrefab.transform.position = Vector3.Lerp(middlePos3d, grid[currentAnt.nextGridPosition.x, currentAnt.nextGridPosition.y].knoten.transform.position, (timer - 0.5f) * 2f);
                        }
                    } else
                    {
                        currentAnt.myPrefab.transform.position = grid[currentAnt.nextGridPosition.x, currentAnt.nextGridPosition.y].knoten.transform.position;
                    }
  

                }


            }
            timer += Time.deltaTime * simulationSpeed;
            yield return null;
        }
        int counter = 0;
        for (int i = 0; i < allAnts.Count; i++)
        {
           
            Ant currentAnt = allAnts[i];
            currentAnt.anim.SetBool("Walk", false);
            currentAnt.anim.SetBool("Fly", false);

            currentAnt.myPrefab.transform.position = grid[currentAnt.nextGridPosition.x, currentAnt.nextGridPosition.y].knoten.transform.position;

            currentAnt.myGridPosition = currentAnt.nextGridPosition;
            
            allAnts[i].myLastPosition = allAnts[i].myPrefab.transform.position;
            counter++;
            if (counter > 250)
            {
                counter = 0;
                yield return null;
            }
            DecisionMaker(currentAnt, grid[currentAnt.myGridPosition.x, currentAnt.myGridPosition.y]);
        }



        isSimulating = false;
        simulationSpeed = uIElements[4].myValue * 5f + 1f;
    }


    void DecisionMaker(Ant ant, Knoten target)
    {
        if (!invincible && !ant.dead)
        {
            ant.lifeTimer--;

            if (ant.lifeTimer < 0)
            {
                
                var prefabPos = ant.myPrefab.transform.position;
                var prefabRot = ant.myPrefab.transform.rotation;

                Destroy(ant.myPrefab);
                ant.myPrefab = Instantiate(deadAntPrefab, prefabPos, prefabRot);
                allAnts.Remove(ant);
                deadAnts.Add(ant);
                ant.dead = true;
                if (ant.deadAnt != -1)
                {
                    Ant currentDeadAnt = deadAnts.SingleOrDefault(x => x.index == ant.deadAnt);
                    currentDeadAnt.myPrefab.transform.SetParent(null);
                    target.deadAnts.Add(currentDeadAnt);
                    ant.deadAnt = -1;
                    
                }
                target.deadAnts.Add(ant);
                ant.myPrefab.SendMessage("ChangeAppearence", isSchematic);

            }

        }
        
        if (ant.dead)
        {
            return;
        }



        target.phearomon = target.deadAnts.Count;
        if (ant.path.Count == 0)
        {
            float prop = 1f;
            prop = ((1f / deadAnts.Count) * target.deadAnts.Count) + alpha;
            //  prop = Mathf.Max(0.0f, Mathf.Min(1.0f, prop));


            if (ant.deadAnt == -1)
            {
                if (target.deadAnts.Count > 0)
                {


                    if (Random.Range(0.0000f, 1.0000f) < 1f - prop)
                    {
                        Ant currentDeadAnt = target.deadAnts[0];
                        ant.deadAnt = currentDeadAnt.index;
                        currentDeadAnt.myPrefab.transform.SetParent(ant.myPrefab.transform);
                        target.deadAnts.RemoveAt(0);
                        ant.myLastPile = new Vector2Int(target.myX, target.myZ);
                        ant.path = FindPath(new Vector2Int(target.myX, target.myZ), checkForPheromon(target, 2));
                        target.phearomon = target.deadAnts.Count;
                    }

                }
                else
                {
                    var newList = checkForDeadAnts(target, 2, true);
                    if (newList.Contains(ant.myLastPile))
                        newList.Remove(ant.myLastPile);
                    if (newList.Count > 0)
                    {
                        ant.path = FindPath(new Vector2Int(target.myX, target.myZ), newList[Random.Range(0, newList.Count)]);

                    }
                }


            }
            else if (ant.deadAnt != -1)
            {


                if (Random.Range(0.0000f, 1.0000f) < prop)
                {
                    Ant currentDeadAnt = deadAnts.SingleOrDefault(x => x.index == ant.deadAnt);

                    ant.deadAnt = -1;
                    currentDeadAnt.myPrefab.transform.SetParent(null);
                    target.deadAnts.Add(currentDeadAnt);
                    target.phearomon = target.deadAnts.Count;
                    ant.myLastPile = new Vector2Int(target.myX, target.myZ);


                    if (target.deadAnts.Count == deadAnts.Count)
                        finised = true;

                    var newList = checkForDeadAnts(target, 2, false);
                    if (newList.Contains(ant.myLastPile))
                        newList.Remove(ant.myLastPile);
                    if (newList.Count > 0)
                    {
      

                        ant.path = FindPath(new Vector2Int(target.myX, target.myZ), newList[Random.Range(0, newList.Count)]);

                    } 
                }
                else
                {
                    var phCheckd = checkForPheromon(target, 2);

                   if (!phCheckd.Equals(ant.myLastPile))
                    {
                        ant.path = FindPath(new Vector2Int(target.myX, target.myZ), phCheckd);
                    }
                       


                   

                }

            }
        }

        target.emission.rateOverTime = Mathf.Clamp(target.phearomon * 10f, 0, 1000);
    }

    public bool Drop()
    {
        return true;
    }

    public List<Vector2Int> checkForDeadAnts(Knoten start, int range, bool onCurrentPosition)
    {
        List<Vector2Int> founds = new List<Vector2Int>();
        for (int r = 0; r < maxSeeRange; r++)
        {
            for (int i = -range - r; i < range + 1 + r; i++)
            {
                for (int i2 = -range - r; i2 < range + 1 + r; i2++)
                {
                    if (!onCurrentPosition && i == 0 && i2 == 0)
                        continue;

                    Vector2Int pos = GetGridPos(start.myX + i, start.myZ + i2);
                    Knoten currentKnoten = grid[pos.x, pos.y];
                    if (currentKnoten.deadAnts.Count > 0)
                    {
                        founds.Add(pos);
                    }
                }
            }

            if (founds.Count >= 0)
                break;
        }
 

        return founds;
    }
    public Vector2Int checkForPheromon(Knoten start, int range)
    {
        Vector2Int found = new Vector2Int(start.myX, start.myZ);
        float maxPheromon = start.phearomon;
        bool foundHiger = false;
        for (int r = range; r < maxSeeRange; r++)
        {
            for (int i = -range -r; i < range + 1 + r; i++)
            {
                for (int i2 = -range - r; i2 < range + 1 + r; i2++)
                {
                    if (i == 0 && i2 == 0)
                        continue;
                    Vector2Int pos = GetGridPos(start.myX + i, start.myZ + i2);
                    Knoten currentKnoten = grid[pos.x, pos.y];
                    if (currentKnoten.phearomon > 0f && maxPheromon <= currentKnoten.phearomon)
                    {
                        found.x = currentKnoten.myX;
                        found.y = currentKnoten.myZ;
                        maxPheromon = currentKnoten.phearomon;
                        foundHiger = true;
                    }
                }
            }
            if (foundHiger)
                break;
        }
        return found;
    }


    Vector2Int NextGridPosition(Vector2Int currentPos, Ant ant)
    {
        Vector2Int direction = Vector2Int.zero;
        if (ant.path.Count > 0 )
        {
            if (ant.searchForNewDead)
            {
                var newList = checkForDeadAnts(grid[currentPos.x, currentPos.y], 2, true);
                if (newList.Contains(ant.myLastPile))
                    newList.Remove(ant.myLastPile);
                if (newList.Count > 0)
                {
                    ant.path = FindPath(currentPos, newList[Random.Range(0, newList.Count)]);
                    ant.searchForNewDead = false;
                }
                
            } 

            direction = ant.path[0];
            ant.path.RemoveAt(0);
        }
        else
        {
            ant.searchForNewDead = !finised;
            Vector2Int ranpPos = new Vector2Int(Random.Range(0, gridSizeX), Random.Range(0, gridSizeY));
            ant.path = FindPath(currentPos, ranpPos);
            direction = ant.path[0];
            ant.path.RemoveAt(0);


        }


        return direction;


    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        // Initialize open and closed lists
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        HashSet<Vector2Int> openSet = new HashSet<Vector2Int>();
        openSet.Add(start);

        // Parent dictionary to track the path
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        // Cost dictionary to track movement cost
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        gScore[start] = 0;

        // Main loop
        while (openSet.Count > 0)
        {
            Vector2Int current = GetLowestFScore(openSet, gScore);
            if (current == end)
            {
                ReconstructPath(cameFrom, path, end);
                return path;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = gScore[current] + CalculateDistance(current, neighbor);
                if (!openSet.Contains(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        path.RemoveAt(0);
        return path;
    }

    // Helper function to get the neighbor nodes
    private List<Vector2Int> GetNeighbors(Vector2Int current)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int x = (current.x + dx) % gridSizeX;
                int y = (current.y + dy) % gridSizeY;

                // Ensure positive values
                x = x < 0 ? gridSizeX + x : x;
                y = y < 0 ? gridSizeY + y : y;

                neighbors.Add(new Vector2Int(x, y));
            }
        }
        return neighbors;
    }

    // Helper function to calculate distance between two points on the grid
    private float CalculateDistance(Vector2Int a, Vector2Int b)
    {
        float dx = Mathf.Min(Mathf.Abs(a.x - b.x), Mathf.Abs(a.x - b.x + gridSizeX), Mathf.Abs(a.x - b.x - gridSizeX));
        float dy = Mathf.Min(Mathf.Abs(a.y - b.y), Mathf.Abs(a.y - b.y + gridSizeY), Mathf.Abs(a.y - b.y - gridSizeY));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // Helper function to get the node with the lowest F score
    private Vector2Int GetLowestFScore(HashSet<Vector2Int> openSet, Dictionary<Vector2Int, float> gScore)
    {
        float minFScore = float.MaxValue;
        Vector2Int minNode = Vector2Int.zero;
        foreach (Vector2Int node in openSet)
        {
            float fScore = gScore[node];
            if (fScore < minFScore)
            {
                minFScore = fScore;
                minNode = node;
            }
        }
        return minNode;
    }

    // Helper function to reconstruct the path
    private void ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, List<Vector2Int> path, Vector2Int current)
    {
        path.Add(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
    }


    public Vector2Int GetGridPos(int x, int z)
    {

        return new Vector2Int((int)Mathf.Repeat(x, gridSizeX), (int)Mathf.Repeat(z, gridSizeY));
    }

    void InitTorus()
    {
        maxSeeRange = Mathf.FloorToInt((float)Mathf.Max(gridSizeX, gridSizeY) * 0.5f);
        int indexCounter = 0;
        grid = new Knoten[gridSizeX, gridSizeY];

        for (int i = 0; i < gridSizeX; i++)
        {
            for (int i2 = 0; i2 < gridSizeY; i2++)
            {
                Vector2 pos = Vector2.zero + Vector2.right * i + Vector2.up * i2;
                Vector2Int intPos = Vector2Int.RoundToInt(pos);
                grid[i, i2] = new Knoten(
                    Instantiate(knotenPrefab, pos, Quaternion.identity),
                    i,
                    i2);
                allObjects.Add(grid[i, i2].knoten);
                int random = Random.Range(0, 100);
                if (random < livingAntChance)
                {
                    int index = indexCounter;
                    Ant newAnt = new Ant(Instantiate(antPrefab, pos, Quaternion.identity), intPos, false, index);
                    grid[i, i2].myAnts.Add(newAnt);
                    allAnts.Add(newAnt);
                    allObjects.Add(newAnt.myPrefab);

                }
                else if (random < livingAntChance + deadAntChance)
                {
                    int index = indexCounter;
                    Ant newAnt = new Ant(Instantiate(deadAntPrefab, pos, Quaternion.identity), intPos, true, index);
                    grid[i, i2].deadAnts.Add(newAnt);
                    deadAnts.Add(newAnt);
                    allObjects.Add(newAnt.myPrefab);

                }
                indexCounter++;
            }
        }


        camCamera.transform.position = grid[Mathf.Clamp(gridSizeX / 2 + 3, 0, gridSizeX-1), gridSizeY / 2].knoten.transform.position - Vector3.forward * 10f;
        camCamera.orthographicSize = Mathf.Max(gridSizeX, gridSizeY) / 2;

        for (int i = 0; i < gridSizeX; i++)
        {
            for (int i2 = 0; i2 < gridSizeY; i2++)
            {
                for (int i3 = -1; i3 < 2; i3++)
                {
                    for (int i4 = -1; i4 < 2; i4++)
                    {
                        Vector2Int homePos = new Vector2Int(i, i2);
                        Vector2Int neighborPos = new Vector2Int((int)Mathf.Repeat(i + i3, gridSizeX), (int)Mathf.Repeat(i2 + i4, gridSizeY));
                        if (Vector2Int.Distance(homePos, neighborPos) < 2)
                        {
                            LineRenderer lr = Instantiate(kantenPrefab);
                            allObjects.Add(lr.gameObject);

                            lr.SetPosition(0, grid[i, i2].knoten.transform.position);
                            lr.SetPosition(1, grid[neighborPos.x, neighborPos.y].knoten.transform.position);
                        }
                    }

                }
            }



        }
        ChangeAntAppearence(isSchematic);
    }


    public void ChangeUiElements (bool init = false)
    {
        
        for (int i = 0; i < uIElements.Length; i++)
        {
            string afterKomma = i == uIElements.Length - 1 ? "F2" : "F0";
            if (!init)
            uIElements[i].myValue = uIElements[i].slider.value;
            else
            uIElements[i].slider.value = uIElements[i].myValue;
            uIElements[i].text.text = uIElements[i].myValue.ToString(afterKomma);

        }
    }

    public void ChangeGraphicsMode(bool schematic)
    {
        graphicBackground.SetActive(!schematic);
        lineMat.SetColor("_Color", schematic ? lineSchematic : lineGraphic);
        ChangeAntAppearence(schematic);
        isSchematic = schematic;
    }

    void ChangeAntAppearence (bool schematic)
    {
        for (int i = 0; i < allAnts.Count; i++)
        {
            allAnts[i].myPrefab.SendMessage("ChangeAppearence", schematic);

        }
        for (int i = 0; i < deadAnts.Count; i++)
        {
            deadAnts[i].myPrefab.SendMessage("ChangeAppearence", schematic);

        }
    }

    public void ToggleInvincibility (bool inv)
    {
        invincible = inv;
    }
   
}

[System.Serializable]
public class Knoten
{
    public int myX, myZ;
    public GameObject knoten;

    public ParticleSystem phParticles;
    public ParticleSystem.EmissionModule emission;
    public List<Ant> myAnts = new List<Ant>();
    public List<Ant> deadAnts = new List<Ant>();
    public float phearomon = 0f;
    public Knoten(GameObject k, int x, int z)
    {
        phParticles = k.GetComponentInChildren<ParticleSystem>();
        emission = phParticles.emission;


        myX = x;
        myZ = z;
        knoten = k;


    }
}

[System.Serializable]
public class Ant
{
    public int lifeTimer;
    public int index;
    public bool dead;
    public bool searchForNewDead;
    public GameObject myPrefab;
    public int deadAnt;
    public LineRenderer lineRenderer;
    public Vector2Int myGridPosition, nextGridPosition;
    public List<Vector2Int> path;
    public Vector2Int myLastPile;
    public Vector3 myLastPosition;
    public Animator anim;
    public Ant(GameObject p, Vector2Int ip, bool d, int i)
    {
        lifeTimer = Random.Range(50, 100);
        searchForNewDead = true;
        path = new List<Vector2Int>();
        p.SendMessage("SetID", i);
 
        index = i;
        deadAnt = -1;
        dead = d;
        myGridPosition = ip;
        myPrefab = p;
        anim = p.GetComponentInChildren<Animator>();

    }
}

[System.Serializable]
public class UIElement
{
    public GameObject main;
    public Slider slider;
    public TMP_Text text;
    public float myValue;
}

