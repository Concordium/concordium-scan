<template>
	<footer class="footer bg-theme-background-primary-elevated-nontrans">
		<div class="footer-powered">
			<div class="text-lg">
				<ConcordiumLogoNoTextIcon class="inline pr-1" /> Powered by Concordium
				Blockchain
			</div>
			<div style="height: 12px" />
			<div class="text-base">
				Creating a future based on trust, privacy and safety
				<br />
				with blockchain technology
			</div>
		</div>
		<div class="footer-links text-base">
			<div class="footer-links-about">
				<OpenLinkButton url="https://www.concordium.com/about">
					About
				</OpenLinkButton>
			</div>
			<div class="footer-links-feedback">
				<OpenLinkButton
					url="https://docs.google.com/forms/d/e/1FAIpQLSc_I5t56NxDfyZV5_204p8NzDDB3SACsA7ZzWDd_6sRkf4tvA/viewform"
				>
					Feedback
				</OpenLinkButton>
			</div>
			<div class="footer-links-privacy">
				<OpenLinkButton url="https://www.concordium.com/privacy-policy">
					Privacy Policy
				</OpenLinkButton>
			</div>
			<div class="footer-links-ccd">
				<OpenLinkButton url="https://www.concordium.com/ccd">
					CCD
				</OpenLinkButton>
			</div>
			<div class="footer-links-contact">
				<OpenLinkButton url="https://www.concordium.com/contact">
					Contact Us
				</OpenLinkButton>
			</div>
			<div class="footer-links-developers">
				<OpenLinkButton
					url="https://developer.concordium.software/en/mainnet/net/guides/developer-page.html"
				>
					Developers
				</OpenLinkButton>
			</div>
		</div>
		<div class="footer-follow text-base0 font-semibold">
			<div class="">Follow us</div>
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
		<div class="footer-rights w-full text-sm">
			CCDScan © 2023 All rights reserved
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
	padding: 50px 130px;
	display: flex;
	flex-wrap: wrap;
	justify-content: space-between;

	@media screen and (max-width: 1280px) {
		padding: 50px 20px;
	}
}

.footer-powered {
	@media screen and (max-width: 1280px) {
		width: 100%;
		padding-bottom: 42px;
	}
}

.footer-rights {
	padding-top: 46px;
}

.footer-follow-title {
	padding-bottom: 18px;
}

.footer-links {
	display: grid;
	padding-top: 6px;
	row-gap: 23px;
	column-gap: 44px;
	grid-template-columns: repeat(3, auto);
	grid-template-rows: repeat(2, auto);

	@media screen and (max-width: 768px) {
		width: 100%;
		padding-bottom: 42px;
		grid-template-columns: repeat(1, auto);
		grid-template-rows: repeat(6, auto);
	}
}

.footer-links-about {
	grid-column: 1 / 2;
	grid-row: 1 / 2;

	@media screen and (max-width: 768px) {
		grid-column: 1 / 2;
		grid-row: 1 / 2;
	}
}

.footer-links-feedback {
	grid-column: 2 / 3;
	grid-row: 1 / 2;

	@media screen and (max-width: 768px) {
		grid-column: 1 / 2;
		grid-row: 2 / 3;
	}
}

.footer-links-privacy {
	grid-column: 3 / 4;
	grid-row: 1 / 2;

	@media screen and (max-width: 768px) {
		grid-column: 1 / 2;
		grid-row: 3 / 4;
	}
}

.footer-links-ccd {
	grid-column: 1 / 2;
	grid-row: 2 / 3;

	@media screen and (max-width: 768px) {
		grid-column: 1 / 2;
		grid-row: 4 / 5;
	}
}

.footer-links-contact {
	grid-column: 2 / 3;
	grid-row: 2 / 3;

	@media screen and (max-width: 768px) {
		grid-column: 1 / 2;
		grid-row: 5 / 6;
	}
}

.footer-links-developers {
	grid-column: 3 / 4;
	grid-row: 2 / 3;

	@media screen and (max-width: 768px) {
		grid-column: 1 / 2;
		grid-row: 6 / 7;
	}
}
</style>
