require("dotenv").config();
const express = require("express");
const bodyParser = require("body-parser");
const TelegramBot = require("node-telegram-bot-api");

const app = express();
const port = process.env.PORT || 3000;

const bot = new TelegramBot(process.env.TELEGRAM_BOT_TOKEN, { polling: true });
const CHAT_ID = process.env.TELEGRAM_CHAT_ID;

app.use(bodyParser.json());

app.post("/initialize", (req, res) => {
	const { address } = req.body;

	if (!address) {
		return res.status(400).send("Address is required");
	}

	bot.sendMessage(CHAT_ID, `New account initialized with address: ${address}`);

	res.status(200).send("Account initialization notified");
});

app.listen(port, () => {
	console.log(`Server running on port ${port}`);
});
