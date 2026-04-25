import OpenAI from "openai";

const fallbackQuestion = {
  category: "history",
  difficulty: "easy",
  question: "Quelle est la capitale du Congo ?",
  options: ["Pointe-Noire", "Brazzaville", "Dolisie", "Owando"],
  correctAnswer: "B",
  explanation: "La capitale du Congo est une grande ville sur le fleuve Congo (rive nord)."
};

export class QuestionGenerator {
  constructor(apiKey) {
    this.enabled = Boolean(apiKey);
    this.client = apiKey ? new OpenAI({ apiKey }) : null;
  }

  async generateOne(language = "fr") {
    if (!this.enabled) return fallbackQuestion;

    const systemPrompt =
      "You are a quiz generator for the Republic of the Congo (country name: Congo). Never use the label 'Congo-Brazzaville'. Return strict JSON only.";
    const userPrompt = `Generate one multiple-choice question in ${language} with this schema:
{
  "category": "history|geography|music|science|culture|languages|people",
  "difficulty": "easy|medium|hard",
  "question": "string",
  "options": ["A option", "B option", "C option", "D option"],
  "correctAnswer": "A|B|C|D",
  "explanation": "short explanation"
}`;

    try {
      const result = await this.client.chat.completions.create({
        model: "gpt-4o-mini",
        temperature: 0.6,
        response_format: { type: "json_object" },
        messages: [
          { role: "system", content: systemPrompt },
          { role: "user", content: userPrompt }
        ]
      });

      return JSON.parse(result.choices[0].message.content);
    } catch (error) {
      console.warn("Question generator fallback:", error?.message || "unknown_error");
      return fallbackQuestion;
    }
  }
}
