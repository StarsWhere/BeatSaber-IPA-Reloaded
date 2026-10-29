using IPA.Config;
using IPA.Loader.Composite;
using IPA.Utilities;
using IPA.Utilities.Async;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.SceneManagement;
// ReSharper disable UnusedMember.Local

namespace IPA.Loader
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class PluginComponent : MonoBehaviour
    {
        private CompositeBSPlugin bsPlugins;
        private CompositeIPAPlugin ipaPlugins;
        private bool quitting;
        public static PluginComponent Instance;
        private static bool initialized = false;

        internal static PluginComponent Create()
        {
            if (Instance != null) return Instance;
            return Instance = new GameObject("IPA_PluginManager").AddComponent<PluginComponent>();
        }

        internal void Awake()
        {
            try { AwakeCore(); }
            catch (Exception failure)
            {
                PluginManager.RecordStartupFailure(failure);
                enabled = false;
                throw;
            }
        }

        private void AwakeCore()
        {
            DontDestroyOnLoad(gameObject);

            if (!initialized)
            {
                UnityGame.SetMainThread();
                UnityGame.EnsureRuntimeGameVersion();
                PluginManager.Load();
            }

            bsPlugins = new CompositeBSPlugin(PluginManager.BSMetas);
#pragma warning disable 618
            ipaPlugins = new CompositeIPAPlugin(PluginManager.Plugins);
#pragma warning restore 618

            if (!initialized)
            {
                bsPlugins.OnEnable();
                ipaPlugins.OnApplicationStart();
            }

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            var unitySched = UnityMainThreadTaskScheduler.Default as UnityMainThreadTaskScheduler;
            if (!unitySched.IsRunning)
                StartCoroutine(unitySched.Coroutine());

            initialized = true;
#if DEBUG
            Config.Stores.GeneratedStoreImpl.DebugSaveAssembly($"{Config.Stores.GeneratedStoreImpl.GeneratedAssemblyName}.dll");
#endif
        }

        internal void Update()
        {
            bsPlugins.OnUpdate();
            ipaPlugins.OnUpdate();

            var unitySched = UnityMainThreadTaskScheduler.Default as UnityMainThreadTaskScheduler;
            if (!unitySched.IsRunning)
                StartCoroutine(unitySched.Coroutine());
        }

        internal void LateUpdate()
        {
            bsPlugins.OnLateUpdate();
            ipaPlugins.OnLateUpdate();
        }

        internal void FixedUpdate()
        {
            bsPlugins.OnFixedUpdate();
            ipaPlugins.OnFixedUpdate();
        }

        internal void OnDestroy()
        {
            DetachSceneEvents();
            if (!ReferenceEquals(Instance, this)) return;
            Instance = null;
            if (!quitting && !PluginManager.StartupFailed)
                Create();
        }

        private void DetachSceneEvents()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        internal void OnApplicationQuit()
        {
            quitting = true;
            if (!initialized) return;
            DetachSceneEvents();
            bsPlugins.OnApplicationQuit();
            ipaPlugins.OnApplicationQuit();
            ConfigRuntime.ShutdownRuntime();
        }

        internal void OnLevelWasLoaded(int level)
        {
            ipaPlugins.OnLevelWasLoaded(level);
        }

        internal void OnLevelWasInitialized(int level)
        {
            ipaPlugins.OnLevelWasInitialized(level);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            bsPlugins.OnSceneLoaded(scene, sceneMode);
        }
        
        private void OnSceneUnloaded(Scene scene) {
            bsPlugins.OnSceneUnloaded(scene);
        }

        private void OnActiveSceneChanged(Scene prevScene, Scene nextScene) {
            bsPlugins.OnActiveSceneChanged(prevScene, nextScene);
        }

    }
}
