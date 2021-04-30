using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BoardGames
{
	public sealed class OfflineChessTurnConfig : MonoBehaviour
	{
		[SerializeField] private Toggle isHumanFirst, isAIFirst, is2Human;
		[SerializeField] private GameObject aiAnchor;
		[SerializeField] private Dropdown aiLevel;
		private static readonly List<Dropdown.OptionData> AI_LEVELS = new List<Dropdown.OptionData>
		{
			new Dropdown.OptionData(nameof(AIAgent.Level.Easy)),
			new Dropdown.OptionData(nameof(AIAgent.Level.Medium)),
			new Dropdown.OptionData(nameof(AIAgent.Level.Hard)),
			new Dropdown.OptionData(nameof(AIAgent.Level.Expert))
		};

		private readonly OfflineTurnManager.Config turnConfig = new OfflineTurnManager.Config();
		private readonly AIAgent.Config aiConfig = new AIAgent.Config();
		private void Awake()
		{
			"TURNBASE_CONFIG".SetValue(turnConfig);
			var isHumanPlayer = turnConfig.isHumanPlayer as IDictionary<int, bool>;
			if (is2Human.isOn)
			{
				isHumanPlayer[0] = isHumanPlayer[1] = true;
				if ("AI_CONFIG".ContainsKey()) "AI_CONFIG".Remove();
				aiAnchor.SetActive(false);
			}
			else
			{
				isHumanPlayer[0] = isHumanFirst.isOn;
				isHumanPlayer[1] = !isHumanPlayer[0];
				"AI_CONFIG".SetValue(aiConfig);
				aiAnchor.SetActive(true);
			}

			isHumanFirst.onValueChanged.AddListener((bool isOn) =>
			{
				isHumanPlayer[0] = isOn;
				isHumanPlayer[1] = !isOn;
			});
			isAIFirst.onValueChanged.AddListener((bool isOn) =>
			{
				isHumanPlayer[0] = !isOn;
				isHumanPlayer[1] = isOn;
			});
			is2Human.onValueChanged.AddListener((bool isOn) =>
			{
				isHumanPlayer[0] = isHumanPlayer[1] = isOn;
				if (isOn)
				{
					if ("AI_CONFIG".ContainsKey()) "AI_CONFIG".Remove();
					aiAnchor.SetActive(false);
				}
				else
				{
					"AI_CONFIG".SetValue(aiConfig);
					aiAnchor.SetActive(true);
				}
			});

			aiLevel.options = AI_LEVELS;
			aiConfig.level = (AIAgent.Level)Enum.Parse(typeof(AIAgent.Level), aiLevel.options[aiLevel.value].text);
			aiLevel.onValueChanged.AddListener(index =>
			{
				aiConfig.level = (AIAgent.Level)Enum.Parse(typeof(AIAgent.Level), aiLevel.options[aiLevel.value].text);
			});
		}
	}
}