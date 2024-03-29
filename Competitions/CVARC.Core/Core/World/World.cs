﻿
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CVARC.V2
{


    public abstract class World<TWorldState> : IWorld
        //where TWorldManager : IWorldManager
        where TWorldState : WorldState
    {
        List<IActor> actors;
        public bool DebugMode { get; protected set; }
        List<IEngine> Engines { get; set; }

        public IdGenerator IdGenerator { get; private set; }
        public WorldClocks Clocks { get; private set; }
        public Scores Scores { get; private set; }
        public LogWriter Logger { get; private set; }
        public GameSettings Configuration { get; private set; }
        public Competitions Competitions { get; private set; }
        public TWorldState WorldState { get; private set; }
		WorldState IWorld.WorldState { get { return WorldState;  } }
        public IKeyboard Keyboard { get; private set; }
        public List<string> LoggingPositionObjectIds { get; private set; }
        public abstract void CreateWorld();

        

        public void OnExit()
        {
            if (Exit != null) Exit();
        }

        public event Action Exit;

        public IEnumerable<IActor> Actors
        {
            get { return actors; }
        }

        public virtual double LoggingPositionTimeInterval
        {
            get
            {
                return 0.1;
            }
        }

        public TEngine GetEngine<TEngine>()
            where TEngine : IEngine
        {
            return (TEngine)Engines.Where(e => e is TEngine).First();
        }

        public virtual void AdditionalInitialization()
        {
        }


        public void Initialize(Competitions competitions, GameSettings configuration, ControllerFactory controllerFactory, WorldState worldState)
        {
            Debugger.Log("World initialization");
            Debugger.Log("Starting basic fields");

            Competitions = competitions;
            Configuration = configuration;
            WorldState = Compatibility.Check<TWorldState>(this, worldState);

            Clocks = new WorldClocks();
            IdGenerator = new IdGenerator();
            Scores = new Scores(this);
            Logger = new LogWriter(this, Configuration.EnableLog, Configuration.LogFile, Configuration, WorldState);
            Keyboard = competitions.KeyboardFactory();
            LoggingPositionObjectIds = new List<string>();

            // setting up the parameters

            Clocks.TimeLimit = Configuration.TimeLimit;


            Debugger.Log("About to init engines");
            //Initializing world
            this.Engines = competitions.EnginesFactory(configuration);
            
            Debugger.Log("Init engines OK");

            Debugger.Log("Complete: basic fields. Starting engine");
            foreach (var engine in Engines) engine.LogWriter = Logger;
            Debugger.Log("Complete: engine. Starting controller factory");
            controllerFactory.Initialize(this);
            Debugger.Log("Complete: controller factory. Creating world");
            CreateWorld();
            Debugger.Log("World created");
            

            //Initializing actors
            actors = new List<IActor>();
            foreach (var id in competitions.Logic.Actors.Keys)
            {
                InitializeActor(
                    competitions,
                    id,
                    competitions.Logic.Actors[id],
                    controllerFactory.Create
                    );
            }

            foreach (var l in competitions.Logic.NPC)
            {
                var f = l.Item3;
                InitializeActor(competitions, l.Item1, l.Item2, (cid, a) => f(a));
                  
            }

            Debugger.Log("Additional world initialization");
            AdditionalInitialization();
        }


        void InitializeActor(Competitions competitions, string id, ActorFactory factory, Func<string,IActor,IController> controllerFactory )
        {
            Debugger.Log("Actor " + id + " initialization");
            Debugger.Log("Creating actor");
            //var factory = competitions.Logic.Actors[id];
            var e = factory.CreateActor();
            var actorObjectId = IdGenerator.CreateNewId(e);
            Debugger.Log("Complete: actor. Creating manager");
            //var manager = competitions.Manager.CreateActorManagerFor(e);
            var rules = factory.CreateRules();
            var preprocessor = factory.CreateCommandFilterSet();
            e.Initialize(/*manager, */this, rules, preprocessor, actorObjectId, id);
            Debugger.Log("Comlete: manager creation. Initializing manager");
            Compatibility.Check<IActor>(this, e);
            Debugger.Log("Comlete: manager initialization. Creating actor body");

            Debugger.Log("Complete: body. Starting controller");

            var controller = controllerFactory(e.ControllerId, e);
            controller.Initialize(e);

            var controlTrigger = new ControlTrigger(controller, e, preprocessor);
            e.ControlTrigger = controlTrigger;

            Clocks.AddTrigger(controlTrigger);
            actors.Add(e);
            Debugger.Log("Actor " + id + " is initialized");
        }


    }
}
