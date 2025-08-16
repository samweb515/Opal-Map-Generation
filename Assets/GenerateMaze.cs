using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CellPair
{
    private GameObject[] cells;
    public int x;
    public int z;
    public CellPair(GameObject cell, GameObject cellWithWalls, int x, int z)
    {
        cells = new GameObject[] { cell, cellWithWalls };
        this.x = x;
        this.z = z;
    }
    public GameObject getWall()
    {
        return cells[1];
    }
    public GameObject getCell(bool hasWalls)
    {
        return cells[hasWalls ? 1 : 0];
    }
    public GameObject[] getNeighbors(bool hasWalls, CellPair[,] mazeCells)
    {
        GameObject[] neighbors = new GameObject[3];
        try { neighbors[0] = mazeCells[x + (hasWalls ? 1 : -1), z + (hasWalls ? 1 : -1)].getCell(!hasWalls); } catch { }
        try { neighbors[1] = mazeCells[x + (hasWalls ? 1 : -1), z].getCell(!hasWalls); } catch { }
        try { neighbors[2] = cells[hasWalls ? 0 : 1]; } catch { }
        return neighbors;
    }
}

public class GenerateMaze : MonoBehaviour
{
    public GameObject Cell;
    public GameObject CellFloor;
    public GameObject Wall0;
    public GameObject Wall1;
    public GameObject Wall2;
    private const int mazesize = 8;
    private float zCellHeight = Mathf.Sqrt(3f);
    private CellPair[,] mazeCells = new CellPair[mazesize * 2 + 1, mazesize * 2];
    private List<GameObject> visited = new List<GameObject>();
    private List<GameObject> structureCells = new List<GameObject>(); // do not use as root of path
    private List<GameObject> edgeOfVisited = new List<GameObject>();
    void Start()
    {
        // create cell grid
        for (int z = 0; z < mazesize * 2; z++)
        {
            for (int x = 0; x < mazesize * 2 + 1; x++)
            {
                if (x == 0 && 0 >= (z - mazesize + 1))
                {
                    GameObject cell = Instantiate(Cell);
                    cell.name = "Cell: " + x + ", " + z;
                    cell.transform.Translate(2 * x - z, 0, zCellHeight * z);
                    CellPair pair = new CellPair(null, cell, x, z);
                    mazeCells[x, z] = pair;
                }
                else if (x == mazesize * 2 && mazesize * 2 <= (mazesize + z))
                {
                    GameObject floor = Instantiate(CellFloor);
                    floor.name = "Floor: " + x + ", " + z;
                    floor.transform.Translate(2 * x - z - 1, 0, zCellHeight * z, Space.World);
                    CellPair pair = new CellPair(floor, null, x, z);
                    mazeCells[x, z] = pair;
                }
                else if (Mathf.Max(0, z - mazesize + 1) <= x && Mathf.Min(mazesize * 2, mazesize + z) >= x)
                {
                    GameObject cell = Instantiate(Cell);
                    cell.name = "Cell: " + x + ", " + z;
                    cell.transform.Translate(2 * x - z, 0, zCellHeight * z);
                    GameObject floor = Instantiate(CellFloor);
                    floor.name = "Floor: " + x + ", " + z;
                    floor.transform.Translate(2 * x - z - 1, 0, zCellHeight * z, Space.World);
                    CellPair pair = new CellPair(floor, cell, x, z);
                    mazeCells[x, z] = pair;
                }
                else
                {
                    mazeCells[x, z] = null;
                }
            }
        }
        for (int i = 0; i < mazesize; i++)
        {
            GameObject wall = Instantiate(Wall0);
            wall.transform.Translate(2 * i + 1, 0, -zCellHeight, Space.World);
            wall = Instantiate(Wall1);
            wall.transform.Translate(mazesize * 3 - i, 0, (mazesize + i) * zCellHeight, Space.World);
            wall = Instantiate(Wall2);
            wall.transform.Translate(i - mazesize, 0, (mazesize + i) * zCellHeight, Space.World);
        }

        // make zones
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i + j != 2 || i == j)
                {
                    int x = (mazesize - 1) * i + 1;
                    int z = (mazesize - 1) * j + 1;
                    directionListConverter(x, z, new int[] { 2, 3, 4, 5, 0, 1 }, true);
                }
            }
        }
        directionListConverter(1, 1, new int[] { 5, 0, 5, 0, 5, 0, 5, 0, 5, 0, 5, 0 }, true);
        directionListConverter(1, 0, new int[] { 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1 }, true);
        directionListConverter(2 * mazesize - 1, 2 * mazesize - 1, new int[] { 5, 4, 5, 4, 5, 4, 5, 4, 5, 4, 5, 4 }, true);
        directionListConverter(2 * mazesize - 1, 2 * mazesize - 2, new int[] { 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3 }, true);
        // make paths from centre to zones
        for (int i = 0; i < 6; i++)
        {
            int x = mazesize + ((i == 2) ? 1 : 0) - ((i == 5) ? 1 : 0);
            int z = (i < 3) ? mazesize : mazesize - 1;
            List<int> path = new List<int> { 5 };
            int deviation = 0;
            int traversal = 0;
            while (traversal < 12)
            {
                if ((deviation + traversal) % 2 == 0)
                {
                    if (path.Last() == 3 || deviation + traversal > 9)
                    {
                        path.Add(4);
                        traversal++;
                    }
                    else if ((!(traversal % 3 == 1 && traversal == 3 * deviation - 2) && Random.Range(0, 2) == 0) || traversal - deviation > 11)
                    {
                        path.Add(0);
                        deviation++;
                    }
                    else
                    {
                        path.Add(4);
                        traversal++;
                    }
                }
                else
                {
                    if (path.Last() == 0 || traversal - deviation > 10)
                    {
                        path.Add(5);
                        traversal++;
                    }
                    else if ((!(traversal % 3 == 1 && traversal == 1 - 3 * deviation) && Random.Range(0, 2) == 0) || deviation + traversal > 10)
                    {
                        path.Add(3);
                        deviation--;
                    }
                    else
                    {
                        path.Add(5);
                        traversal++;
                    }
                }
            }

            for (int j = 0; j < path.Count; j++)
            {
                path[j] = (path[j] + i) % 6;
            }

            directionListConverter(x, z, path.ToArray().Take(path.ToArray().Length - 1).ToArray(), false, i);
        }

        //true random generation
        while (edgeOfVisited.Count > 0)
        {
            int randomIndex = Random.Range(0, edgeOfVisited.Count);
            GameObject cell = edgeOfVisited[randomIndex];
            List<GameObject> neighbors = new List<GameObject>();
            string[] parts = cell.name.Split(':');
            bool hasWalls = parts[0] == "Cell";
            string coordsPart = parts[1].Trim();
            parts = coordsPart.Split(',');
            int x = int.Parse(parts[0].Trim());
            int z = int.Parse(parts[1].Trim());
            CellPair pair = mazeCells[x, z];
            foreach (GameObject neighbor in pair.getNeighbors(hasWalls, mazeCells))
            {
                if (neighbor != null) { if (visited.Contains(neighbor)) neighbors.Add(neighbor); }
            }
            GameObject visitedCell = neighbors[Random.Range(0, neighbors.Count)];
            parts = visitedCell.name.Split(':');
            coordsPart = parts[1].Trim();
            parts = coordsPart.Split(',');
            int vx = int.Parse(parts[0].Trim());
            int vz = int.Parse(parts[1].Trim());
            CellPair vpair = mazeCells[vx, vz];

            if (vz > z)
            {
                //Debug.Log("Down");
                directionConverter(vx, vz, 3);
            }
            else if (z > vz)
            {
                //Debug.Log("Up");
                directionConverter(vx, vz, 0);
            }
            else if (vx > x)
            {
                //Debug.Log("Left");
                directionConverter(vx, vz, 5);
            }
            else if (x > vx)
            {
                //Debug.Log("Right");
                directionConverter(vx, vz, 2);
            }
            else
            {
                //Debug.Log("Stay");
                directionConverter(vx, vz, hasWalls ? 1 : 4);
            }
        }

        foreach (CellPair pair in mazeCells)
        {
            //try { Debug.Log(pair.x + ", " + pair.z); } catch { }
            try
            {
                //Debug.Log(pair.getCell(false).GetComponent<ICell>().branch);
                pair.getCell(false).GetComponent<Renderer>().material.color = Color.HSVToRGB(pair.getCell(false).GetComponent<ICell>().branch / 7f, 1f, 1f);
            }
            catch { }
            try
            {
                //Debug.Log(pair.getCell(true).GetComponent<ICell>().branch);
                pair.getCell(true).transform.Find("CellFloor").GetComponent<Renderer>().material.color = Color.HSVToRGB(pair.getCell(true).GetComponent<ICell>().branch / 7f, 1f, 1f);
            }
            catch { }
        }

        for (int j = 0; j < 2; j++)
        {
            List<GameObject> doorCell = new List<GameObject>();
            List<int> doorDirection = new List<int>();
            foreach (CellPair pair in mazeCells)
            {
                try
                {
                    if (pair.getCell(true).GetComponent<ICell>().branch == j * 3)
                    {
                        GameObject[] tmp = pair.getNeighbors(true, mazeCells);
                        for (int i = 0; i < 3; i++)
                        {
                            {
                                if (
                                    pair.getCell(true).GetComponent<ICell>().branch + 1 == tmp[i].GetComponent<ICell>().branch
                                    && (!(pair.x <= 11 && pair.x >= 4 && pair.z <= 11 && pair.z >= 4)
                                        || (pair.x + pair.z >= 19)
                                        || (pair.x + pair.z < 11)
                                    )
                                )
                                {
                                    doorCell.Add(tmp[i]);
                                    doorDirection.Add((2 * i + 3) % 6);
                                }
                            }
                        }
                    }
                }
                catch { }
                try
                {
                    if (pair.getCell(false).GetComponent<ICell>().branch == j * 3)
                    {
                        GameObject[] tmp = pair.getNeighbors(false, mazeCells);
                        for (int i = 0; i < 3; i++)
                        {
                            {
                                if (
                                    pair.getCell(false).GetComponent<ICell>().branch + 1 == tmp[i].GetComponent<ICell>().branch
                                    && (!(pair.x <= 11 && pair.x >= 4 && pair.z <= 11 && pair.z >= 4)
                                        || (pair.x + pair.z > 19)
                                        || (pair.x + pair.z <= 11)
                                    )
                                )
                                {
                                    doorCell.Add(tmp[i]);
                                    doorDirection.Add(2 * i);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            int doorNumber = Random.Range(0, doorDirection.Count);
            string[] parts = doorCell[doorNumber].name.Split(':');
            bool hasWalls = parts[0] == "Cell";
            string coordsPart = parts[1].Trim();
            parts = coordsPart.Split(',');
            int x = int.Parse(parts[0].Trim());
            int z = int.Parse(parts[1].Trim());
            directionConverter(x, z, doorDirection[doorNumber], false);
        }
        
    }

    private void checkNeighbors(GameObject cell)
    {
        string[] parts = cell.name.Split(':');
        bool hasWalls = parts[0] == "Cell";
        string coordsPart = parts[1].Trim();
        parts = coordsPart.Split(',');
        int x = int.Parse(parts[0].Trim());
        int z = int.Parse(parts[1].Trim());
        CellPair pair = mazeCells[x, z];
        foreach (GameObject neighbor in pair.getNeighbors(hasWalls, mazeCells))
        {
            if (neighbor != null) { if (!edgeOfVisited.Contains(neighbor) && !visited.Contains(neighbor) && !structureCells.Contains(neighbor)) edgeOfVisited.Add(neighbor); }
        }
    }

    private void breakWall(int x1, int z1, int x2, int z2)
    {
        // cell with walls is on pos x and z
        //Debug.Log("breaking: " + x1 + ", " + z1 + ", " + x2 + ", " + z2);
        try
        {
            if (z1 > z2)
            {
                //Debug.Log("Down");
                Destroy(mazeCells[x2, z2].getWall().transform.Find("CellWall0")?.gameObject);
                if (mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch != -1)
                {
                    mazeCells[x2, z2].getCell(true).GetComponent<ICell>().branch = mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch;
                }
                else
                {
                    mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch = mazeCells[x2, z2].getCell(true).GetComponent<ICell>().branch;
                }
            }
            else if (z2 > z1)
            {
                //Debug.Log("Up");
                Destroy(mazeCells[x1, z1].getWall().transform.Find("CellWall0")?.gameObject);
                if (mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch != -1)
                {
                    mazeCells[x2, z2].getCell(false).GetComponent<ICell>().branch = mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch;
                }
                else
                {
                    mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch = mazeCells[x2, z2].getCell(false).GetComponent<ICell>().branch;
                }
            }
            else if (x1 > x2)
            {
                //Debug.Log("Left");
                Destroy(mazeCells[x2, z2].getWall().transform.Find("CellWall2")?.gameObject);
                if (mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch != -1)
                {
                    mazeCells[x2, z2].getCell(true).GetComponent<ICell>().branch = mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch;
                }
                else
                {
                    mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch = mazeCells[x2, z2].getCell(true).GetComponent<ICell>().branch;
                }
            }
            else if (x2 > x1)
            {
                //Debug.Log("Right");
                Destroy(mazeCells[x1, z1].getWall().transform.Find("CellWall2")?.gameObject);
                if (mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch != -1)
                {
                    mazeCells[x2, z2].getCell(false).GetComponent<ICell>().branch = mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch;
                }
                else
                {
                    mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch = mazeCells[x2, z2].getCell(false).GetComponent<ICell>().branch;
                }
            }
            else
            {
                //Debug.Log("Stay");
                Destroy(mazeCells[x1, z1].getWall().transform.Find("CellWall1")?.gameObject);
                if (mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch != -1)
                {
                    mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch = mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch;
                }
                else
                {
                    mazeCells[x1, z1].getCell(false).GetComponent<ICell>().branch = mazeCells[x1, z1].getCell(true).GetComponent<ICell>().branch;
                }
            }
        }
        catch (System.Exception)
        {
        }
    }

    private int[] directionConverter(int x, int z, int direction, bool structure = false)
    {
        /* clockwise 6 directions
         * up: 0
         * up-right: 1
         * down-right: 2
         * down: 3
         * down-left: 4
         * up-left: 5
        */
        if (direction % 3 == 1)
        {
            breakWall(x, z, x, z);
            try
            {
                GameObject cell = mazeCells[x, z].getCell(direction == 1);
                if (!visited.Contains(cell))
                {
                    if (structure) { structureCells.Add(cell); }
                    else
                    {
                        visited.Add(cell);
                        checkNeighbors(cell);
                    }
                    if (edgeOfVisited.Contains(cell)) edgeOfVisited.Remove(cell);
                }
            }
            catch { }
        }
        else if (direction == 0)
        {
            breakWall(x, z, x + 1, z + 1);
            try
            {
                GameObject cell = mazeCells[x + 1, z + 1].getCell(false);
                if (!visited.Contains(cell))
                {
                    if (structure) { structureCells.Add(cell); }
                    else
                    {
                        visited.Add(cell);
                        checkNeighbors(cell);
                    }
                    if (edgeOfVisited.Contains(cell)) edgeOfVisited.Remove(cell);
                }
            }
            catch { }
            z++;
            x++;
        }
        else if (direction == 3)
        {
            breakWall(x, z, x - 1, z - 1);
            try
            {
                GameObject cell = mazeCells[x - 1, z - 1].getCell(true);
                if (!visited.Contains(cell))
                {
                    if (structure) { structureCells.Add(cell); }
                    else
                    {
                        visited.Add(cell);
                        checkNeighbors(cell);
                    }
                    if (edgeOfVisited.Contains(cell)) edgeOfVisited.Remove(cell);
                }
            }
            catch { }
            z--;
            x--;
        }
        else if (direction == 2)
        {
            breakWall(x, z, x + 1, z);
            try
            {
                GameObject cell = mazeCells[x + 1, z].getCell(false);
                if (!visited.Contains(cell))
                {
                    if (structure) { structureCells.Add(cell); }
                    else
                    {
                        visited.Add(cell);
                        checkNeighbors(cell);
                    }
                    if (edgeOfVisited.Contains(cell)) edgeOfVisited.Remove(cell);
                }
            }
            catch { }
            x++;
        }
        else if (direction == 5)
        {
            breakWall(x, z, x - 1, z);
            try
            {
                GameObject cell = mazeCells[x - 1, z].getCell(true);
                if (!visited.Contains(cell))
                {
                    if (structure) { structureCells.Add(cell); }
                    else
                    {
                        visited.Add(cell);
                        checkNeighbors(cell);
                    }
                    if (edgeOfVisited.Contains(cell)) edgeOfVisited.Remove(cell);
                }
            }
            catch { }
            x--;
        }

        return new int[] { x, z };
    }
    private void directionListConverter(int xStart, int zStart, int[] listOfDirections, bool structure = false, int path = -1) {
        int x = xStart;
        int z = zStart;
        mazeCells[x, z].getCell(listOfDirections[0] % 2 == 0).GetComponent<ICell>().branch = path;
        foreach (int direction in listOfDirections)
        {
            int[] newXZ = directionConverter(x, z, direction, structure);
            x = newXZ[0];
            z = newXZ[1];
        }
    }
}
