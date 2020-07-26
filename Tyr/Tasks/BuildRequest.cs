﻿using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;

namespace SC2Sharp.Tasks
{
    public class BuildRequest
    {
        public uint Type;
        public Point2D Pos;
        public Agent worker;
        public Base Base;
        public Point2D AroundLocation;
        public bool Exact;
        public int LastImprovementFrame;
        public float Closest;
    }
}