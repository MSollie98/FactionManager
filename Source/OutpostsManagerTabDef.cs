using System;
using System.Collections.Generic;
using FactionManager.ModSupport;
using RimWorld.Planet;
using Syrchalis_SetUpCamp;
using UnityEngine;
using Verse;

namespace FactionManager
{
	public class OutpostsManagerTabDef : ManagerTabDef
	{
		public override void DrawManagerRect(Rect outRect, ref Vector2 scrollPosition, ref float scrollViewHeight)
		{
			base.DrawManagerRect(outRect, ref scrollPosition, ref scrollViewHeight);
			List<MapParent> list = Find.World.worldObjects.MapParents.FindAll((MapParent item) => (item != null && item.def == SetUpCampDefOf.CaravanCamp));
			GUI.BeginGroup(outRect);
			Rect rect = new Rect(0f, 0f, outRect.width, outRect.height);
            Rect rect2 = new Rect(0f, 0f, outRect.width, scrollViewHeight);
			float num = 0f;
			Widgets.BeginScrollView(rect, ref scrollPosition, rect2, true);
			foreach (MapParent outpost in list)
			{
				GUI.color = new Color(1f, 1f, 1f, 0.2f);
				Widgets.DrawLineHorizontal(0f, num + 50f, rect2.width);
				OutpostsManagerTabDef.DrawOutpostRow(outpost, num, rect2);
				num += 50f;
			}
			bool flag = Event.current.type == EventType.Layout;
			if (flag)
			{
				scrollViewHeight = num;
			}
			Widgets.EndScrollView();
			GUI.EndGroup();
		}

		private static float DrawOutpostRow(MapParent outpost, float rowY, Rect fillRect)
		{
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect rect = new Rect(0f, rowY, fillRect.width * 0.7f, 50f);
			Rect rect2 = new Rect(10f, rowY, rect.width - 10f, 50f);
			string outpostName = outpost.LabelCap + " " + outpost.Tile.ToString();
			Widgets.Label(rect2, outpostName);
			float num = (fillRect.width - rect.xMax) * 0.2f;
			float num2 = fillRect.width - num - (rect.width + num);
			Rect rect3 = new Rect(rect.width + num, rowY + 5f, num2, 40f);
			string buttonText = outpost.HasMap ? "FM.Unload" : "FM.Load";
			string text = Translator.Translate(buttonText);
			bool flag = Widgets.ButtonText(rect3, text, true, false, true);
			if (flag)
			{
				System.Action action = delegate()
				{
					bool flag5 = buttonText == "FM.Load";
					if (flag5)
					{
						LongEventHandler.QueueLongEvent(delegate()
						{
							PersistenceUtility.LoadMap(outpostName, true);
						}, "LoadingLongEvent", true, null);
					}
					else
					{
						LongEventHandler.QueueLongEvent(delegate()
						{
							PersistenceUtility.UnloadMap(outpost.Map, outpostName, true);
						}, "SavingLongEvent", false, null);
					}
					Find.WindowStack.TryRemove(typeof(MainTabWindow_Colonies), true);
				};
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(Translator.Translate("FM.actionConfirm") + Translator.Translate(buttonText).ToLower() + "?", action, false, null));
			}
			Rect rect4 = new Rect(0f, rowY, fillRect.width, 50f);
			bool flag2 = Mouse.IsOver(rect4);
			if (flag2)
			{
				GUI.DrawTexture(rect4, TexUI.HighlightTex);
			}
			bool flag3 = Widgets.ButtonInvisible(rect4, false);
			if (flag3)
			{
				bool flag4 = !Mouse.IsOver(rect3);
				if (flag4)
				{
					Find.WindowStack.TryRemove(typeof(MainTabWindow_Colonies), true);
					CameraJumper.TryJumpAndSelect(outpost);
				}
			}
			Text.Anchor = 0;
			return rowY;
		}
		public override bool doLoad()
		{
			return SetUpCampSupport.SetupCampActive();
		}
	}
}
