﻿using System;

namespace NadekoBot.Common
{
    public static class PlatformHelper
    {
        private const int ProcessorCountRefreshIntervalMs = 30000;

        private static volatile int _processorCount;
        private static volatile int _lastProcessorCountRefreshTicks;

        public static int ProcessorCount {
            get {
                var now = Environment.TickCount;
                if (_processorCount == 0 || (now - _lastProcessorCountRefreshTicks) >= ProcessorCountRefreshIntervalMs)
                {
                    _processorCount = Environment.ProcessorCount;
                    _lastProcessorCountRefreshTicks = now;
                }

                return _processorCount;
            }
        }
    }
}