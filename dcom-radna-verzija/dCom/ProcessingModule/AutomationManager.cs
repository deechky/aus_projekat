using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;


namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;
        private EGUConverter eguConverter = new EGUConverter();


        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;

        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


        private void AutomationWorker_DoWork()
        {

            while (!disposedValue)
            {
                automationTrigger.WaitOne();
                IPoint waterLevelPoint = storage.GetPoints(new List<PointIdentifier> { new PointIdentifier(PointType.ANALOG_OUTPUT, 1000) }).First();
                IPoint temperaturePoint = storage.GetPoints(new List<PointIdentifier> { new PointIdentifier(PointType.ANALOG_OUTPUT, 1001) }).First();
                IPoint heaterPoint = storage.GetPoints(new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 2002) }).First();
                IPoint valvePoint = storage.GetPoints(new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 2000) }).First();

                IAnalogPoint waterLevel = waterLevelPoint as IAnalogPoint;
                IAnalogPoint temperature = temperaturePoint as IAnalogPoint;
                IDigitalPoint heater = heaterPoint as IDigitalPoint;
                IDigitalPoint valve = valvePoint as IDigitalPoint;

                // simulacija promjene temperature
                if (heater.State == DState.ON)
                {
                    double heatingConst = 0;

                    if (temperature.EguValue < 30)
                    {
                        heatingConst = 2;
                    }
                    else if (temperature.EguValue >= 30 && temperature.EguValue <= 50)
                    {
                        heatingConst = 5;
                    }
                    else
                    {
                        heatingConst = 20;
                    }

                    double newTemp = temperature.EguValue + heatingConst;
                    processingManager.ExecuteWriteCommand(temperature.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1001, (int)eguConverter.ConvertToRaw(temperature.ConfigItem.ScaleFactor, temperature.ConfigItem.Deviation, newTemp));
                }

                // provjera praga pozara i automatska akcija
                if (temperature.EguValue > 57)
                {
                    processingManager.ExecuteWriteCommand(valve.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2000, (int)DState.ON);
                }

                if (valve.State == DState.ON)
                {
                    double coolingPerSecond = 4.0;
                    double outflowPerSecond = 10.0;

                    double currentWaterLevel = waterLevel.EguValue;
                    double currentTemp = temperature.EguValue;

                    if (currentWaterLevel > 0)
                    {
                        double waterToUse = Math.Min(currentWaterLevel, outflowPerSecond);

                        double newWaterLevel = currentWaterLevel - waterToUse;
                        double coolingEffect = (waterToUse / outflowPerSecond) * coolingPerSecond;
                        double newTemp = currentTemp - coolingEffect;

                        processingManager.ExecuteWriteCommand(waterLevel.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1000, (int)eguConverter.ConvertToRaw(waterLevel.ConfigItem.ScaleFactor, waterLevel.ConfigItem.Deviation, newWaterLevel));
                        processingManager.ExecuteWriteCommand(temperature.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1001, (int)eguConverter.ConvertToRaw(temperature.ConfigItem.ScaleFactor, temperature.ConfigItem.Deviation, newTemp));

                    }

                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
