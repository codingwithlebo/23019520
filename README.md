# ⚡ FastPay

> **Tip creators. Pay freelancers. Donate to causes.**
> Human-readable payments on Solana — no wallet addresses needed.

---

## 🧠 The Problem

Web3 wallet addresses look like this:

```
7xKp3mNqRs8vBtWzYeLdFgHjKoMnPqRs3mNq
```

One wrong character and your money is gone forever. This is the #1 reason everyday people don't use crypto for payments like tipping a creator, paying a freelancer, or donating to a cause.

## ✅ Our Solution

FastPay abstracts wallet addresses into:

- **Human-readable usernames** → `@malebo`
- **Simple payment links** → `fastpay.id/@malebo`
- **Dynamic QR codes** → scan and pay in one tap via Phantom

No errors. No confusion. One click.

---

## 🚀 Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18 + Vite |
| Styling | Tailwind CSS v3 |
| Blockchain | Solana (Devnet) |
| Wallet | Phantom Wallet |
| Icons | Tabler Icons React |

---

## 🎯 Use Cases

- 🎨 **Creator tipping** — streamers, YouTubers, artists receive SOL tips via QR on screen
- 💼 **Freelancer invoicing** — share a payment link in WhatsApp, no bank details needed
- 🌍 **Donations** — charities and causes accept borderless crypto donations instantly

---

## 📁 Project Structure

```
src/
├── components/
│   ├── Topbar.jsx          # Navigation bar + Phantom connect
│   ├── Sidebar.jsx         # Left nav + wallet balance card
│   ├── PhantomModal.jsx    # Animated wallet connection flow
│   └── SuccessOverlay.jsx  # Payment success screen
├── pages/
│   ├── TipPage.jsx         # Search user + send tip flow
│   ├── Dashboard.jsx       # Earnings charts + activity feed
│   ├── History.jsx         # Full transaction table
│   ├── Profile.jsx         # User identity settings
│   └── QRPage.jsx          # QR code + dynamic invoice
├── data/
│   └── users.js            # Mock user + transaction data
├── App.jsx                 # Root component + page routing
└── index.css               # Tailwind directives + custom classes
```

---

## 🛠️ Getting Started

### Prerequisites
- Node.js v18+
- npm v9+
- Git

### Installation

```bash
# Clone the repo
git clone https://github.com/codingwithlebo/fastpay-ui.git

# Navigate into the project
cd fastpay-ui

# Install dependencies
npm install

# Start the dev server
npm run dev
```

Open **http://localhost:5173** in your browser.

---

## 🎮 Demo Flow

1. Go to **Send Tip** page
2. Search for `@malebo`, `@dev_rizky`, or `@jacob_codes`
3. Select an amount and click **Send via Phantom**
4. Watch the success screen — transaction confirmed on Solana devnet
5. Check **Dashboard** for live charts and activity
6. Check **QR Code** page to see your shareable payment link

---

## 🗺️ Roadmap

- [x] Frontend UI — React + Tailwind
- [x] Username search + profile lookup
- [x] Phantom wallet connection flow
- [x] QR code generation
- [ ] Real Phantom wallet integration (`@solana/web3.js`)
- [ ] Username → wallet address database (Supabase)
- [ ] On-chain transaction signing
- [ ] Mainnet deployment

---

## 👥 Team

Built at the **Dev3pack Global Web3 Hackathon 2026**

| Name | Role |
|------|------|
| Malebo Nkuna | Frontend · UI/UX · Pitch |
| Rizky Januar | Solana · Smart Contracts |
| Jacob Mensah | Backend · APIs · n8n |

---

## 📄 License

MIT — free to use, modify, and build on.

---

> *"The best Web3 UX is the one that doesn't feel like Web3 at all."*
