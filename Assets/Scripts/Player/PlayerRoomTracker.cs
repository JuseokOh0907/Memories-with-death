using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 플레이어가 지금 어느 방에 있는지 기억한다.
    /// 문턱에서는 두 방의 트리거가 겹치므로 "겹친 방 목록" 을 들고,
    /// 새 방에 들어가면 그 방, 현재 방에서 나가면 아직 겹쳐 있는 방으로 넘어간다.
    /// 모든 방 밖으로 나가도 마지막 방을 유지한다.
    /// </summary>
    public class PlayerRoomTracker : MonoBehaviour
    {
        private readonly List<Room> overlapping = new List<Room>();

        public Room Current { get; private set; }

        /// <summary>(이전 방, 새 방). 첫 진입이면 이전 방이 null.</summary>
        public event Action<Room, Room> RoomChanged;

        internal void NotifyEnter(Room room)
        {
            if (!overlapping.Contains(room))
                overlapping.Add(room);
            SetCurrent(room);
        }

        internal void NotifyExit(Room room)
        {
            overlapping.Remove(room);
            if (Current == room && overlapping.Count > 0)
                SetCurrent(overlapping[overlapping.Count - 1]);
        }

        private void SetCurrent(Room room)
        {
            if (room == Current)
                return;

            Room previous = Current;
            Current = room;
            RoomChanged?.Invoke(previous, room);
        }
    }
}
