using System.Collections;
using System.Collections.Generic;
using UFLevelStructure;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class RoomSensor : MonoBehaviour {

    public UFPlayerInfo info { get {
            return UFLevel.playerInfo;
    } }

}

#if UNITY_EDITOR
[CustomEditor(typeof(RoomSensor))]
public class RoomSensorEditor : Editor {
     
    private static int findID;

    private RoomSensor sensor { get { return (RoomSensor)target; } }

    public override void OnInspectorGUI() {

        DrawDefaultInspector();

        if(sensor.info == null) {
            GUILayout.Label("No UF level found, cannot find room data.");
            return;
        }


        List<Room> rooms = GetContainedRooms();
        int nboRooms = rooms.Count;

        if(nboRooms <= 0)
            GUILayout.Label("No room");
        else if(nboRooms == 1)
            GUILayout.Label("Unique room");
        else
            GUILayout.Label("Multiple rooms");

        foreach(Room room in rooms) {
            GUILayout.Label(room.ToString());
        }
    }

    void OnSceneGUI() {
        List<Room> rooms = GetContainedRooms();

        Handles.color = Color.red;

        foreach(Room room in rooms) {
            for(int i = 0; i < 8; i++) {
                Vector3 b = Vector3.zero;
                float l;
                for(int j = 0; j < 3; j++)
                    b[j] = ((i >> j) & 1) == 0 ? room.aabb.min[j] : room.aabb.max[j];
                for(int j = 0; j < 3; j++) {
                    l = (room.aabb.max[j] - room.aabb.min[j]) / 2f;
                    Vector3 dir = Vector3.zero;
                    dir[j] = ((i >> j) & 1) == 0 ? 1f : -1f;
                    Handles.DrawLine(b, b + l * dir);
                }
            }

            Handles.color = Color.yellow;
        }
    }



    private List<Room> GetContainedRooms() {
        List<Room> toReturn = new List<Room>();
        Room[] rooms = sensor.info.rooms;

        for(int i = rooms.Length - 1; i >= 0; i--) {
            if(rooms[i].aabb.IsInside(sensor.transform.position))
                toReturn.Add(rooms[i]);
        }

        return toReturn;
    }
}


#endif
