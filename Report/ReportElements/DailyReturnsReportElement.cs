﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deedle;
using Python.Runtime;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class DailyReturnsReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new plot of the daily returns in bar chart format
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public DailyReturnsReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the annual returns plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestReturns = Calculations.EquityPoints(_backtest);
            var liveReturns = Calculations.EquityPoints(_live);

            var backtestSeries = new Series<DateTime, double>(backtestReturns.Keys, backtestReturns.Values);
            var liveSeries = new Series<DateTime, double>(liveReturns.Keys, liveReturns.Values);

            // The following two operations are equivalent to the Pandas `DataFrame.resample(...)` method
            var backtestResampled = backtestSeries.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum()) * 100;
            var liveResampled = liveSeries.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum()) * 100;

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                backtestList.Append(backtestResampled.Keys.ToList().ToPython());
                backtestList.Append(backtestResampled.Values.ToList().ToPython());

                var liveList = new PyList();
                liveList.Append(liveResampled.Keys.ToList().ToPython());
                liveList.Append(liveResampled.Values.ToList().ToPython());

                base64 = Charting.GetDailyReturns(backtestList, liveList);

                backtestList.Dispose();
                liveList.Dispose();
            }

            return base64;
        }
    }
}