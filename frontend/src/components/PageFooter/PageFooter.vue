<template>
	<footer class="footer bg-theme-background-primary-elevated-nontrans">
		<div class="footer-rights text-sm">CCDScan © 2023 All rights reserved</div>
		<div class="footer-powered">
			<div class="text-lg">
				<ConcordiumLogoNoTextIcon class="inline pr-1" /> Powered by Concordium
				Blockchain
			</div>
			<div style="height: 12px"></div>
			<div class="text-base">
				Creating a future based on trust, privacy and safety
				<br />
				with blockchain technology
			</div>
		</div>
		<div class="footer-links text-base">
			<div class="footer-about-about">
				<OpenLinkButton url="https://www.concordium.com/about">
					About
				</OpenLinkButton>
			</div>
			<div class="footer-about-feedback">
				<OpenLinkButton
					url="https://docs.google.com/forms/d/e/1FAIpQLSc_I5t56NxDfyZV5_204p8NzDDB3SACsA7ZzWDd_6sRkf4tvA/viewform"
				>
					Feedback
				</OpenLinkButton>
			</div>
			<div class="footer-about-privacy">
				<OpenLinkButton url="https://www.concordium.com/privacy-policy">
					Privacy Policy
				</OpenLinkButton>
			</div>
			<div class="footer-about-ccd">
				<OpenLinkButton url="https://www.concordium.com/ccd">
					CCD
				</OpenLinkButton>
			</div>
			<div class="footer-about-contact">
				<OpenLinkButton url="https://www.concordium.com/contact">
					Contact Us
				</OpenLinkButton>
			</div>
			<div class="footer-about-developers">
				<OpenLinkButton
					url="https://developer.concordium.software/en/mainnet/net/guides/developer-page.html"
				>
					Developers
				</OpenLinkButton>
			</div>
		</div>
		<div class="footer-follow text-base0 font-semibold">
			Follow us
			<div style="height: 18px"></div>
			<div>
				<OpenLinkButton url="https://twitter.com/ConcordiumNet">
					<XIcon class="inline w-12 h-12" />
				</OpenLinkButton>
				<OpenLinkButton url="https://www.linkedin.com/company/concordium/">
					<LinkedinIcon class="inline w-12 h-12" />
				</OpenLinkButton>
				<OpenLinkButton url="https://t.me/concordium_official">
					<TelegramIcon class="inline w-12 h-12" />
				</OpenLinkButton>
				<OpenLinkButton url="https://discord.com/invite/GpKGE2hCFx">
					<DiscordIcon class="inline w-12 h-12" />
				</OpenLinkButton>
				<OpenLinkButton url="https://github.com/Concordium">
					<GithubIcon class="inline w-12 h-12" />
				</OpenLinkButton>
				<OpenLinkButton url="https://medium.com/@concordium">
					<MediumIcon class="inline w-12 h-12" />
				</OpenLinkButton>
			</div>
		</div>
	</footer>
</template>
<script lang="ts" setup>
import GithubIcon from '.././icons/GithubIcon.vue'
import ConcordiumLogoNoTextIcon from '.././icons/ConcordiumLogoNoTextIcon.vue'
import XIcon from '.././icons/XIcon.vue'
import LinkedinIcon from '.././icons/LinkedinIcon.vue'
import TelegramIcon from '.././icons/TelegramIcon.vue'
import DiscordIcon from '.././icons/DiscordIcon.vue'
import MediumIcon from '.././icons/MediumIcon.vue'
import OpenLinkButton from './OpenLinkButton.vue'
</script>
<style>
.footer {
	display: grid;
	position: fixed;
	left: 0;
	bottom: 0;
	width: 100%;
	height: 210px;
	grid-template-columns: repeat(6, auto);
	grid-template-rows: repeat(7, auto);
	column-gap: 150px;
	row-gap: 20px;
	padding: 50px 130px 0;

	@media screen and (max-width: 961px) {
		height: 200px;
	}

	@media screen and (max-width: 640px) {
		height: 300px;
	}
}
.footer-about-about {
	grid-column: 1 / 2;
	grid-row: 1 / 4;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 1 / 2;
	}
}
.footer-about-feedback {
	grid-column: 2 / 3;
	grid-row: 1 / 4;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 2 / 3;
	}
}
.footer-about-privacy {
	grid-column: 3 / 4;
	grid-row: 1 / 4;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 3 / 4;
	}
}
.footer-about-ccd {
	grid-column: 1 / 2;
	grid-row: 4 / 7;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 4 / 5;
	}
}
.footer-about-contact {
	grid-column: 2 / 3;
	grid-row: 4 / 7;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 5 / 6;
	}
}
.footer-about-developers {
	grid-column: 3 / 4;
	grid-row: 4 / 7;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 6 / 7;
	}
}

.footer-powered {
	grid-column: 1 / 3;
	grid-row: 1 / 2; /* 7*/

	@media screen and (max-width: 961px) {
		grid-column: 1 / 4;
		grid-row: 1 / 4;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 1 / 3;
	}
}
.footer-links {
	grid-column: 3 / 5;
	grid-row: 1 / 2;
	display: grid;
	grid-template-columns: repeat(3, auto);
	grid-template-rows: repeat(6, auto);
	padding-top: 6px;
	row-gap: 23px;
	column-gap: 44px;

	@media screen and (max-width: 961px) {
		grid-column: 1 / 4;
		grid-row: 4 / 7;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 3 / 5;
	}
}
.footer-follow {
	grid-column: 5 / 7;
	grid-row: 1 / 7;

	@media screen and (max-width: 961px) {
		grid-column: 4 / 7;
		grid-row: 1 / 7;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 5 / 7;
	}
}
.footer-rights {
	grid-column: 1 / 7;
	grid-row: 2 / 8;

	/* @media screen and (max-width: 961px) {
		grid-column: 1 / 7;
		grid-row: 7 / 9;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 5 / 7;
	} */
}
</style>
