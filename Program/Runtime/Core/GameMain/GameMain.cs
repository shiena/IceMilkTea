﻿// zlib/libpng License
//
// Copyright (c) 2018 Sinoa
//
// This software is provided 'as-is', without any express or implied warranty.
// In no event will the authors be held liable for any damages arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it freely,
// subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software.
//    If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace IceMilkTea.Core
{
    /// <summary>
    /// ゲームメインクラスの実装をするための抽象クラスです。
    /// IceMilkTeaによるゲームのスタートアップからメインループを構築する場合は必ず継承し実装をして下さい。
    /// </summary>
    public abstract class GameMain : ScriptableObject
    {
        /// <summary>
        /// ゲームサービスマネージャのサービス起動ルーチンを実行する型です
        /// </summary>
        public struct GameServiceManagerStartup { }


        /// <summary>
        /// ゲームサービスマネージャのサービス終了ルーチンを実行する型です
        /// </summary>
        public struct GameServiceManagerCleanup { }



        /// <summary>
        /// 現在のゲームメインコンテキストを取得します
        /// </summary>
        public static GameMain CurrentContext { get; private set; }


        /// <summary>
        /// 現在のゲームメインが保持しているサービスマネージャを取得します
        /// </summary>
        public GameServiceManager ServiceManager { get; private set; }



        /// <summary>
        /// Unity起動時に実行されるゲームのエントリポイントです
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Main()
        {
            // ゲームメインをロードする
            CurrentContext = LoadGameMain();


            // サービスマネージャのインスタンスを生成するが、nullが返却されるようなことがあれば
            CurrentContext.ServiceManager = CurrentContext.CreateGameServiceManager();
            if (CurrentContext.ServiceManager == null)
            {
                // ゲームシステムは破壊的な死亡をした
                throw new InvalidOperationException("GameServiceManager の正しいインスタンスが生成されませんでした。");
            }


            // アプリケーションの終了イベントを引っ掛けておく
            Application.quitting += CurrentContext.Shutdown;


            // ゲームループの開始と終了のタイミングあたりにサービスマネージャのスタートアップとクリーンアップの処理を引っ掛ける
            var loopSystem = ImtPlayerLoopSystem.GetLastBuildLoopSystem();
            loopSystem.InsertLoopSystem<Initialization.PlayerUpdateTime, GameServiceManagerStartup>(InsertTiming.AfterInsert, StartupGameService);
            loopSystem.InsertLoopSystem<PostLateUpdate.ExecuteGameCenterCallbacks, GameServiceManagerCleanup>(InsertTiming.AfterInsert, CleanupGameService);
            loopSystem.BuildAndSetUnityDefaultPlayerLoop();


            // ゲームの起動を開始する
            CurrentContext.Startup();
        }


        /// <summary>
        /// ゲームメインをロードします
        /// </summary>
        /// <returns>ロードされたゲームメインを返します</returns>
        private static GameMain LoadGameMain()
        {
            // 内部で保存されたGameMainのGameMainをロードする
            var gameMain = Resources.Load<GameMain>("GameMain");


            // ロードが出来なかったのなら
            if (gameMain == null)
            {
                // セーフ起動用のゲームメインで立ち上げる
                return CreateInstance<SafeGameMain>();
            }


            // リダイレクトするGameMainがあるか聞いて、存在するなら
            var redirectGameMain = gameMain.RedirectGameMain();
            if (redirectGameMain != null)
            {
                // ロードされたGameMainを解放して、リダイレクトされたGameMainを設定する
                Destroy(gameMain);
                gameMain = redirectGameMain;
            }


            // ロードしたゲームメインを返す
            return gameMain;
        }


        /// <summary>
        /// ゲームサービスマネージャのサービススタートアップ処理を呼び出します
        /// </summary>
        private static void StartupGameService()
        {
            // スタートアップをする
            CurrentContext.ServiceManager.StartupServices();
        }


        /// <summary>
        /// ゲームサービスマネージャ
        /// </summary>
        private static void CleanupGameService()
        {
            // クリーンアップをする
            CurrentContext.ServiceManager.CleanupServices();
        }


        /// <summary>
        /// 起動するGameMainをリダイレクトします。
        /// IceMilkTeaによって起動されたGameMainから他のGameMainへリダイレクトする場合は、
        /// この関数をオーバーライドして起動するGameMainのインスタンスを返します。
        /// </summary>
        /// <returns>リダイレクトするGameMainがある場合はインスタンスを返しますが、ない場合はnullを返します</returns>
        protected virtual GameMain RedirectGameMain()
        {
            // リダイレクト先GameMainはなし
            return null;
        }


        /// <summary>
        /// ゲームサービスを管理する、サービスマネージャを生成します。
        /// ゲームサービスの管理をカスタマイズする場合は、
        /// この関数をオーバーライドしてGameServiceManagerを継承したクラスのインスタンスを返します。
        /// </summary>
        /// <returns>GameServiceManager のインスタンスを返します</returns>
        protected virtual GameServiceManager CreateGameServiceManager()
        {
            // 通常は内部ゲームサービスマネージャを生成して返す
            return new InternalGameServiceManager();
        }


        /// <summary>
        /// ゲームの起動処理を行います。
        /// 主に、ゲームサービスの初期登録や必要な追加モジュールの初期化などを行います。
        /// </summary>
        protected virtual void Startup()
        {
        }


        /// <summary>
        /// ゲームの終了処理を行います。
        /// ゲームサービスそのものの終了処理は、サービス側で処理されるべきで、
        /// この関数では主に、追加モジュールなどの解放やサービス管轄外の解放などを行うべきです。
        /// </summary>
        protected virtual void Shutdown()
        {
        }
    }
}